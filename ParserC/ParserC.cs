
using System.Text;
using System.Text.RegularExpressions;
using HeaderTranslator.ParserC.Constructions;

namespace HeaderTranslator.ParserC;


public class Parser
{
    private string headerSource;
    private string notPreprocessed;
    public List<Struct> Structures { get; set; } = new List<Struct>();
    public List<TypeDefinition> Typedefs { get; set; } = new List<TypeDefinition>();
    public List<Type> Types { get; set; }= new List<Type>();
    public List<Function> Functions { get; set; } = new List<Function>();
    public List<Union> Unions { get; private set; } = new List<Union>();

    public List<KeyValuePair<string, string>> StringConstantas = new List<KeyValuePair<string, string>>();
    public List<KeyValuePair<string, string>> IntConstantas = new List<KeyValuePair<string, string>>();

    public IConstruction[] Constructions => 
        Structures.Select(C => (IConstruction)C)
        .Concat(Typedefs.Select(C => (IConstruction)C))
        .Concat(Functions.Select(C => (IConstruction)C))
        .Concat(Unions.Select(C => (IConstruction)C)).ToArray();
    public Parser(string headerSource, string notPreprocessed)
    {
        this.notPreprocessed = notPreprocessed;
        this.headerSource = headerSource;
        this.headerSource = new Regex("(?<!(?:\".*))//.*").Replace(this.headerSource, "");//remove comments
        this.headerSource = new Regex(@"#.*").Replace(this.headerSource, "");//remove compiller comments
        this.headerSource = new Regex(@"[\s\n\t]").Replace(this.headerSource, " ");
        this.headerSource = new Regex(@"\s+").Replace(this.headerSource, " ");
        this.headerSource = new Regex(@"((?:/[*]).*?)([*]/)").Replace(this.headerSource, "");
        this.headerSource = new Regex(@"\bconst\b").Replace(this.headerSource, "");
        this.headerSource = new Regex(@"\bout\b").Replace(this.headerSource, "out_");
        this.headerSource = new Regex(@"\bref\b").Replace(this.headerSource, "ref_");
        this.headerSource = new Regex(@"\bclass\b").Replace(this.headerSource, "class_");
        this.headerSource = new Regex(@"\bevent\b").Replace(this.headerSource, "event_");
        this.headerSource = new Regex(@"\bstring\b").Replace(this.headerSource, "string_");
    }

    public void Parse()
    {
        ParseCommonTypesDefinitions();
        ParseStructures();
        ParseFunctionTitles();
        ParseConstants();
    }

    private void ParseConstants()
    {
        var matches = Regex.Matches(notPreprocessed, 
        $"#\\s*define\\s+(?<name>\\w+)\\s+(?<value>(.+))\n");

        foreach (Match match in matches)
        {
            string name = match.Groups["name"].Value;
            string value = match.Groups["value"].Value;

            if(Regex.IsMatch(value, $"\".*\""))
            {
                if(value.Where(ch => ch == '\"').Count() > 1)
                {
                    var m = Regex.Matches(value, RInMatchedBlock("\"", "\"", "[^\"]+")); 
                    if(m.Count() !=0)
                    {
                        StringConstantas.Add(new KeyValuePair<string, string>(name.Trim(), m.First().Value));
                    }
                }
            }
            else if( !value.Contains("define") && value.Where(ch => char.IsDigit(ch)).Count() != 0)
            {
                IntConstantas.Add(new KeyValuePair<string, string>(name, value));
            }
        }
    }

    private void ParseFunctionTitles()
    {
        var matches = Regex.Matches(headerSource, 
        $"(?<extern>extern\\s+)?(?<static>static\\s+)?(?<struct>struct\\s+)?(?<type>(?<unsigned>unsigned\\s+)?(?<signed>signed\\s+)?(?<short>short\\s+)?(?<long>long\\s+)*(?<type_name>{RSymbolName}))(?:(?:\\s+)|(?<ptr>(?:\\s*[*]\\s*)+))?(?<name>{RSymbolName})\\s*(?<args>{RInMatchedBlock(@"\(", @"\)",@"[^()]+")})\\s*;");

        foreach (Match match in matches)
        {
            string @extern = match.Groups["extern"].Value;
            string @static = match.Groups["static"].Value;
            
            string unsigned = match.Groups["unsigned"].Value;
            string signed = match.Groups["signed"].Value;
            string @short = match.Groups["short"].Value;
            string @long = match.Groups["long"].Value;

            string type_name = match.Groups["type_name"].Value;

            if(new string[]{
                "return"
                }.Contains(type_name.Trim())) continue;

            string ptr = match.Groups["ptr"].Value;
            string name = match.Groups["name"].Value;
            string args = match.Groups["args"].Value;

            Type returnType = new Type(
                type_name, 
                ptr.Where(ch => ch == '*').Count(), 
                string.IsNullOrEmpty(unsigned) == false, 
                string.IsNullOrEmpty(signed) == false, 
                string.IsNullOrEmpty(@short) == false, 
                string.IsNullOrEmpty(@long) == false
            );
            List<Variable> functionArgs = new List<Variable>();

            var argMatches = Regex.Matches(args,
            $"(?<struct>struct\\s+)?(?<type>(?<unsigned>unsigned\\s+)?(?<signed>signed\\s+)?(?<short>short\\s+)?(?<long>long\\s+)*(?<type_name>{RSymbolName}))(?:(?:\\s+)|(?<ptr>(?:\\s*[*]\\s*)+))(?<arg_name>{RSymbolName})?");

            foreach (Match argMatch in argMatches)
            {
                string arg_name = argMatch.Groups["arg_name"].Value;
                string arg_type = argMatch.Groups["type"].Value;
                string arg_type_name = argMatch.Groups["type_name"].Value;
                string arg_ptr = match.Groups["ptr"].Value;

                string arg_struct = argMatch.Groups["struct"].Value;
                string arg_unsigned = argMatch.Groups["unsigned"].Value;
                string arg_signed = argMatch.Groups["signed"].Value;
                string arg_short = argMatch.Groups["short"].Value;
                string arg_long = argMatch.Groups["long"].Value;


                Type argumentType = new Type(
                    arg_type_name, 
                    arg_ptr.Where(ch => ch == '*').Count(),
                    string.IsNullOrEmpty(arg_unsigned) == false, 
                    string.IsNullOrEmpty(arg_signed) == false, 
                    string.IsNullOrEmpty(arg_short) == false, 
                    string.IsNullOrEmpty(arg_long) == false
                );

                functionArgs.Add(new Variable(argumentType, string.IsNullOrEmpty(arg_name)?null:arg_name ));
            }

            Function newFunction = new Function(returnType, name, functionArgs.ToArray());
            Functions.Add(newFunction);
        }
    }
    private void ParseCommonTypesDefinitions()
    {
        var matches = Regex.Matches(headerSource, 
        $"(?<typedef>typedef\\s+)(?<struct>struct\\s+)?(?<type>(?<unsigned>unsigned\\s+)?(?<signed>signed\\s+)?(?<short>short\\s+)?(?<long>long\\s+)*(?<type_name>{RSymbolName}))(?:(?:\\s*)|(?<ptr>(?:\\s*[*]\\s*)+))(?<def_name>{RSymbolName})\\s*;");

        foreach (Match match in matches)
        {
            if(match.ToString().Contains("XID"))
            {

            }

            string typedef = match.Groups["typedef"].Value;
            string def_name = match.Groups["def_name"].Value;
            string type = match.Groups["type"].Value;
            string type_name = match.Groups["type_name"].Value;
            string ptr = match.Groups["ptr"].Value;

            string @struct = match.Groups["struct"].Value;
            string unsigned = match.Groups["unsigned"].Value;
            string signed = match.Groups["signed"].Value;
            string @short = match.Groups["short"].Value;
            string @long = match.Groups["long"].Value;
            


            Type toDef = new Type(
                type_name,
                ptr.Where(ch => ch == '*').Count(),
                string.IsNullOrEmpty(unsigned) == false, 
                string.IsNullOrEmpty(signed) == false, 
                string.IsNullOrEmpty(@short) == false, 
                string.IsNullOrEmpty(@long) == false
            );

            TypeDefinition newTypedefinition = new TypeDefinition(def_name, toDef);
            Typedefs.Add(newTypedefinition);
            Types.Add(newTypedefinition.DefinedType);
        }

    }

    private void ParseStructures()
    {
        var matches = Regex.Matches(headerSource, 
        $"(?<typedef>typedef\\s*)?(?<struct>struct\\s+)(?<name>{RSymbolName}\\s+)?(?<body>{RInMatchedBlock()})\\s*(?<def_name>{RSymbolName})?\\s*;*");

        foreach (Match match in matches)
        {
            string typedef = match.Groups["typedef"].Value;
            string def_name = match.Groups["def_name"].Value;
            string name = match.Groups["name"].Value;

            Struct? newStruct = ParseStruct(match.ToString());
            if(newStruct != null)
            {

                if(string.IsNullOrEmpty(typedef) == false)
                {
                    TypeDefinition newTypedef = new TypeDefinition(def_name, newStruct);
                    Typedefs.Add(newTypedef);
                    Types.Add(newTypedef.DefinedType);

                }
            }
        }
    }

    private Struct? ParseStruct(string structString)
    {
        var matches = Regex.Matches(structString, 
        $"(?<struct>struct\\s+)(?<name>{RSymbolName}\\s+)?(?<body>{RInMatchedBlock()})");
        Struct? newStruct = null;

        
        if(matches.Count() != 0)
        {
            Match match = matches.First();

            string name = match.Groups["name"].Value;
            string body = match.Groups["body"].Value;

            newStruct = new Struct(GetStructureArgs(body), string.IsNullOrEmpty(name) ? null : name);
        }

        return newStruct;
    }

    private int counter = 0;

    private Variable[] GetStructureArgs(string body)
    {
        List<Variable> variables = new List<Variable>();

        var matches = Regex.Matches(body,
        $"(?:(?<struct>struct\\s+)|(?<union>union\\s+))(?<type_name>{RSymbolName}\\s+)?(?<body>{RInMatchedBlock()})\\s*(?<name>{RSymbolName})\\s*(?:\\[\\s*(?<array_len>\\d)\\s*\\])?\\s*;"); 

        variables.AddRange(GetCommonVariables(body));
            
        foreach (Match match in matches)
        {
            string @struct = match.Groups["struct"].Value;
            string name = match.Groups["name"].Value;
            string type_name = match.Groups["type_name"].Value;

            if(string.IsNullOrEmpty(@struct) == false)
            {
                Struct? newStruct = ParseStruct(match.ToString());
                if(newStruct != null)
                {
                    Structures.Add(newStruct);
                    string structName = string.IsNullOrEmpty(type_name) ? $"Type_{counter++}_{name}" : type_name;
                    Type newVariableType = new TypeDefinition(structName, newStruct).DefinedType;
                    Types.Add(newVariableType);

                    variables.Add(new Variable(newVariableType, name));
                }
            }
            else
            {
                string union_body = match.Groups["body"].Value;
                Union newUnion = new Union(
                    string.IsNullOrEmpty(type_name) ? $"Type_{counter++}_{name}": type_name,
                    GetCommonVariables(body)
                );

                Unions.Add(newUnion);
                Type newType = new Type(newUnion.Name);

                variables.Add(new Variable(newType, name));
            }
        }

        return variables.ToArray();
    }

    private Variable[] GetCommonVariables(string body)
    {
        List<Variable> variables = new List<Variable>();

        var matches1 = Regex.Matches(body,
        $"(?<type>(?<unsigned>unsigned\\s+)?(?<signed>signed\\s+)?(?<short>short\\s+)?(?<long>long\\s+)*(?<type_name>{RSymbolName}))(?:(?:\\s*)|(?<ptr>(?:\\s*[*]\\s*)+))(?<name>{RSymbolName})\\s*(?:\\[\\s*(?<array_len>\\d)\\s*\\])?\\s*;"); 

        foreach (Match match in matches1)
        {
            string type = match.Groups["type"].Value;
            string unsigned = match.Groups["unsigned"].Value;
            string signed = match.Groups["signed"].Value;
            string @short = match.Groups["short"].Value;
            string @long = match.Groups["long"].Value;
            string type_name = match.Groups["type_name"].Value;
            string ptr = match.Groups["ptr"].Value;
            string name = match.Groups["name"].Value;
            string array_len = match.Groups["array_len"].Value;


            variables.Add(new Variable(
                    new Type(
                        type_name,
                        ptr.Where(ch => ch == '*').Count(),
                        string.IsNullOrEmpty(unsigned) == false, 
                        string.IsNullOrEmpty(signed) == false, 
                        string.IsNullOrEmpty(@short) == false, 
                        string.IsNullOrEmpty(@long) == false
                    ),
                    name,
                    string.IsNullOrEmpty(array_len)? 1 : int.Parse(array_len)
                )
            );
        }

        return variables.ToArray();
    }

    private const string RSymbolName = @"(?:[A-Za-z_$]+([A-Za-z0-9_$]*?)(?![A-Za-z0-9_$]))";
    private string RInMatchedBlock(string openBlock = @"\{", string closeBlock = @"\}", string body = @"[^{}]+")
    {
        return $"(?:{openBlock}(?>{openBlock}(?<c>)|(?:{body})+|{closeBlock}(?<-c>))*(?(c)(?!)){closeBlock})";
    }
}