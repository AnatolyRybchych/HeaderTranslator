
using System.Text.RegularExpressions;
using HeaderTranslator.ParserC;
using HeaderTranslator.ParserC.Constructions;

namespace HeaderTranslator;

using NewType = System.Int32;

public class CSWriter
{
    public class ConstructionName
    {
        public string Name { get; private set; }
        public IConstruction Construction { get; private set; }
        public ConstructionName(IConstruction construction, string name)
        {
            Name = name;
            Construction = construction;
        }
    }

    private string OutClass;
    private string InLibrary;
    public List<ConstructionName> WritedConstructions { get; private set;} = new List<ConstructionName>();
    public List<ConstructionName> NotWritedConstructions { get; private set;} = new List<ConstructionName>();

    public CSWriter(string outClass = "Lib", string inputLibrary = "lib.so")
    {
        OutClass = outClass;
        InLibrary = inputLibrary;

        WritedConstructions.AddRange(new string[]{
            "int",
            "uint",
            "short",
            "ushort",
            "long",
            "ulong",
            "byte",
            "sbyte",
        }.Select(constr => new ConstructionName(new TypeDefinition(constr, new ParserC.Type(constr)), constr)));
    }

    public string Write(Parser parser)
    {
        return
        $"using System.Runtime.InteropServices;\n\n" + 
        WriteTypes(parser.Typedefs)+ "\n\n" +
        $"public static class {OutClass}{{\n" +

        Tabulate(
            $"public const string LibPath = \"{InLibrary}\";\n\n\n" +
            WriteStringConstants(parser.StringConstantas) +
            WriteIntConstants(parser.IntConstantas) + "\n\n" +
            WriteFunctions(parser.Functions) +
            WriteStructures(parser.Structures) + 
            WriteUnions(parser.Unions)
        ) +
        $"}}";
    }

    private string WriteUnions(List<Union> unions)
    {
        return string.Join("", unions.Select(union => $"{WriteUnion(union)}\n\n"));
    }

    private string WriteStringConstants(List<KeyValuePair<string, string>> stringConstants)
    {
        string result = "";
        foreach (var strConst in stringConstants)
        {
            result += $"public const string {strConst.Key} = {strConst.Value};\n";
        }
        return result;
    }

    private string WriteIntConstants(List<KeyValuePair<string, string>> intConstants)
    {
        string result = "";
        foreach (var intConst in intConstants)
        {
            result += $"public const long {intConst.Key} = {intConst.Value};\n";
        }
        return result;
    }

    private object WriteUnion(Union union)
    {
        if(WasWriten(union) == false)
        {
            return 
            $"[StructLayout(LayoutKind.Explicit)] \n" +
            $"public unsafe struct {union.Name} {{{WriteVariables(union.Fields, "", (field, id) => $"\n    [FieldOffset(0)] public unsafe {WriteVariable(field, id)};" )};\n}}";
        }
        else
        {
            return "";
        }
    }

    private string WriteTypes(List<TypeDefinition> typedefs)
    {
        string res = "";
        foreach (var typedef in typedefs)
        {
            if(typedef.Body != null)
            {
                res += WriteStructure(new Struct(typedef.Body.Args, typedef.Name)) + "\n\n";
            }
            else if(typedef.Pseudoname != null)
            {
                WriteTypedef(typedef, ref res, typedefs);
            }
        }

        return res;
    }

    private void WriteTypedef(TypeDefinition typedef, ref string result,List<TypeDefinition>typedefs)
    {
        var defined = typedefs.Where( td => td.DefinedType.Name.Trim() == typedef.Pseudoname.Name.Trim());
        if(defined.Count() != 0)
        {
            WriteTypedef(new TypeDefinition(typedef.DefinedType.Name, new ParserC.Type(defined.First().Pseudoname.Name)), ref result, typedefs);
        }
        else
        {
            result  += $"using  {WriteType(typedef.DefinedType)} = {WriteType(typedef.Pseudoname)};\n";
        }
    }

    public string Tabulate(string src)
    {
        return string.Join("", src.Split('\n').Select(str => $"    {str}\n"));
    }

    public string WriteStructures(IEnumerable<Struct> structures)
    {
        return string.Join("", structures.Select(structure => $"{WriteStructure(structure)}\n\n"));
    }

    public string WriteStructure(Struct structure)
    {
        if(WasWriten(structure) == false)
        {
            var res = $"public unsafe struct {structure.Name} {{{WriteVariables(structure.Args, "", (arg, id) => $"\n    public unsafe {WriteVariable(arg, id)};" )}\n}}";
            return res;
        }
        else 
        {
            return "";
        }
    }
    public string WriteFunctions(IEnumerable<Function> funcs)
    {
        return string.Join("", funcs.Select(func => $"{WriteFunction(func)}\n\n"));
    }

    public string WriteFunction(Function func)
    {
        if(WasWriten(func) == false)
        {
            string result = $"[DllImport(LibPath)]\n" +
            $"public static unsafe extern {WriteType(func.ReturnType)} {func.Name} ({WriteVariables(func.Arguments, ",", ((arg , id)=> $" {WriteVariable(arg, id)}"))});";
            return result; 
        }
        else
        {
            return "";
        }
    }

    public string WriteVariables(Variable[] vars, string separator , Func<Variable, int, string>? eachVar)
    {
        string result = string.Join(separator, vars.Select((var, i) => eachVar?.Invoke(var, i)));;
        return result;
    }
    public string WriteVariable(Variable var, int number)
    {
        string fx = $"{(var.ArrayLength == 1?"":"fixed ")}";
        string arr = $"{(var.ArrayLength == 1?"":$"[{var.ArrayLength}]")}";
        return $"{fx}{WriteType(var.Type)} {var.Name ?? $"arg{number}"}{arr}";
    }

    public bool WasWriten(IConstruction construction)
    {
        if(WritedConstructions
            .Select(construction => construction.Name)
            .Contains(construction.Name)
            || construction.Name == null
        )
        {
            NotWritedConstructions.Add(new ConstructionName(construction, construction?.Name ?? ""));
            return true;
        }
        else
        {
            WritedConstructions.Add(new ConstructionName(construction, construction?.Name ?? ""));
            return false;
        }
    }

    public string WriteType(HeaderTranslator.ParserC.Type type)
    {
        
        string res = "";
        if(type.Name == "int")
        {
            if(type.Unsigned) res += "System.U";
            else res += "System.";

            if(type.Short) res += "Int16";
            else if(type.Long) res += "Int64";
            else  res += "Int32";
        }
        else if(type.Name == "char")
        {
            if(type.Unsigned) res += "System.Byte";
            else res += "System.SByte";
        }
        else if(type.Name == "short")
        {
            if(type.Unsigned) res += "System.UInt16";
            else res += "System.Int16";
        }
        else if(type.Name == "long")
        {
            if(type.Unsigned) res += "System.UInt64";
            else res += "System.Int64";
        }
        else if(type.Name == "char"  || type.Name == "double")
        {
            res +=  "System."+char.ToUpper(type.Name[0]) + type.Name.Substring(1);
        }
        else if(type.Name == "float")
        {
            res += "System.Single";
        }
        else if(type.Name == "double")
        {
            res += "System.Double";
        }
        else if(type.Name == "void")
        {
            res += type.Name;
        }
        else
        {
            if(type.PointerVolume == 0)
            {
                return type.Name;
            }
            else
            {
                return $"System.IntPtr /*|{type.Name}{new string('*', type.PointerVolume)}|*/ ";
            }
        }
        return (type.PointerVolume == 0 ? res : $"System.IntPtr /*{res}*/");
    }
}