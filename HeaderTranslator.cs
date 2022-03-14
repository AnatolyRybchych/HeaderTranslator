

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HeaderTranslator;

public class HeaderTranslator
{
    private string cSource;
    private string cSourceFile;

    public string ErrorBuffer{ get; private set; } = "";

    public HeaderTranslator(string cHeaderSource, string headerFileName)
    {
        cSourceFile = headerFileName;
        cSource = cHeaderSource;
    }

    public string Translate()
    {
        StringBuilder className = new StringBuilder(cSourceFile);
        className[0] = char.ToUpper(className[0]);

        return

        "using System.Runtime.InteropServices;\n" +
        "\n" +
        "\n" +
        $"public static class {className}\n" +
        "{\n" + 
            $"\tpublic const string lib = \"{cSourceFile}\";\n" +
            "\t\n" + 
            String.Join("\n    ",TranslateBody().Split('\n')) +
        "}\n\n\n";
    }

    private bool isSpace(char ch){
        return char.IsWhiteSpace(ch);
    }

    private string TranslateBody()
    {
        StringBuilder tmp = new StringBuilder("");

        for (int i = 0; i < cSource.Length - 1; i++)
        {
            if((isSpace(cSource[i]) && isSpace(cSource[i+1])) == false){
                tmp.Append(cSource[i]);
            }
        }
        string src = tmp.ToString();
        
        src = Regex.Replace(src, @"#(.*?)(?=\n)\n", "");
        src = Regex.Replace(src, @"\bconst\b", "");
        src = Regex.Replace(src, @"\bunsigned[\s\r]*long[\s\r]*int\b", "ulong");
        src = Regex.Replace(src, @"\bunsigned[\s\r]*int\b", "uint");
        src = Regex.Replace(src, @"\blong[\s\r]*int\b", "long");
        src = Regex.Replace(src, @"\bparams\b", "@params");
        src = Regex.Replace(src, @"\bsigned\s*char\b", "sbyte");
        src = Regex.Replace(src, @"\bunsigned\s*char\b", "byte");
        src = Regex.Replace(src, @"\bchar\b", "sbyte");
        src = Regex.Replace(src, @"\bref\b", "@ref");
        src = Regex.Replace(src, @"\bout\b", "@out");
        src = Regex.Replace(src, @"\([\s\r]*void[\s\r]*\)", "()");
        src = Regex.Replace(src, @"\bstring\b", "@string");
        src = Regex.Replace(src, @"\bevent\b", "@event");
        src = Regex.Replace(src, @"\bobject\b", "@object");
        src = Regex.Replace(src, @"\bin\b", "@in");
        src = Regex.Replace(src, @"\bbase\b", "@base");
        src = Regex.Replace(src, @"\bsigned[\s\r]*long[\s\r]*int\b", "long");
        src = Regex.Replace(src, @"\bsigned[\s\r]*long\b", "long");
        src = Regex.Replace(src, @"\bunsigned[\s\r]*long\b", "ulong");
        src = Regex.Replace(src, @"\bunsigned[\s\r]*short[\s\r]*int\b", "ushort");
        src = Regex.Replace(src, @"\bunsigned[\s\r]*short\b", "ushort");
        src = Regex.Replace(src, @"\bsigned[\s\r]*short\b", "short");
        src = Regex.Replace(src, @"\bunsigned[\s\r]*short[\s\r]*int\b", "short");
        src = Regex.Replace(src, @"\bshort[\s\r]*int\b", "short");
        src = Regex.Replace(src, @"\bsigned[\s\r]*int\b", "int");

        TranslateArrays(ref src);

        string result = "";

        result += TranslateExternFunctionPointers(ref src);
        result += TranslateFunctionDefinitions(ref src);
        result += TranslateFunctionTitles(ref src);
        result += TranslateTypes(ref src);

        return result;
    }

    public void TranslateArrays(ref string src){
        Regex regex = new Regex(@"(?<=\s)([\w_]+?)(?=\[)\[(?<=\[)((?:\s*\d+\s*)+?)(?=\])\]");
        var matches = regex.Matches(src);

        foreach (Match match in matches)
        {
            if(match.Groups.Count < 3) continue;
            
            Group all = match.Groups[0];
            Group name = match.Groups[1];
            Group size = match.Groups[2];
            src = src.Replace(all.Value, $"*{name.Value}/*[{size.Value}]*/");
            
        }
    }


    private string TranslateTypes(ref string src)
    {
        Regex typeDefRegex = new Regex(@"typedef\s+(.+?)(?=(?:[\w_]+;))([\w_]+);");
        StringBuilder result = new StringBuilder("");

        foreach (Match match in typeDefRegex.Matches(src))
        {
            var groups = match.Groups;
            if(groups.Count < 3) ErrorBuffer += match.Value + "\n";
            else result.Append(TranslateType(match));
        }

        src = typeDefRegex.Replace(src, "");
        return result.ToString();
    }

    private string TranslateType(Match match)
    {
        var groups = match.Groups;
        var typeBody = groups[1].Value;
        var typeName = groups[2].Value;
        var @unsafe = "";

        if(groups.Count < 3)
        {
            ErrorBuffer += match.Value + "\n";
            return "";
        }
        else
        {
            if(typeName.Contains("struct")) return "";
            if(typeBody.Contains('*')) @unsafe = "unsafe";
            if(typeBody.Contains('{') && typeBody.Contains('}')) return $"public {@unsafe} {typeBody}";

            typeBody = typeBody.Replace("struct", "");

            return $"public {@unsafe} struct {typeName}{"{\n"}\tpublic {typeBody} value;\n  {"\n}"}";
        }
    }


    private string TranslateFunctionDefinitions(ref string src){
        Regex funDefRegex = new Regex(@"typedef[\w\s_]*\([\w\s*_]+\)\s*\([\w\s*,_()]*\)");
        StringBuilder result = new StringBuilder("");

        foreach (var match in funDefRegex.Matches(src))
        {
            string? title = match?.ToString();
            if(title != null)
                result.Append(TranslateFunctionDefinition(title));
        }

        src = funDefRegex.Replace(src, "");
        return result.ToString();
    }

    private string TranslateFunctionDefinition(string title){
        string titleCp = title.Replace("struct", "");

        titleCp = titleCp.Replace("typedef", "");

        Regex nameRegex = new Regex(@"\(\s*\*[\w\s_]+\s*\)");
        var matches = nameRegex.Matches(titleCp);
        if(matches.Count != 0){
            var match = matches.First();
            string name = match.ToString();
            titleCp = titleCp.Replace(match.ToString(), 
                name.Replace("(", "").Replace(")", ""). Replace("*", ""));
        }
        else
        {
            ErrorBuffer += titleCp + "\n";
            return "";
        }

        string @unsafe = titleCp.Contains('*') ? "unsafe" : "";

        return 
        "\n" +
        $"public {@unsafe} delegate {titleCp};\n";
    }

    private string TranslateExternFunctionPointers(ref string src){
        Regex funPtrRegex = new Regex(@"extern[\w\s_]*\([\w\s*_]+\)\s*\([\w\s*,_()]*\)");
        StringBuilder result = new StringBuilder("");

        foreach (var match in funPtrRegex.Matches(src))
        {
            string? title = match?.ToString();
            if(title != null)
                result.Append(TranslateExternFunctionPointer(title));
            
        }

        src = funPtrRegex.Replace(src, "");
        return result.ToString();
    }

    private string TranslateExternFunctionPointer(string title){
        string titleCp = title.Replace("struct", "");

        titleCp = titleCp.Replace("extern", "");

        Regex nameRegex = new Regex(@"\([^)]*\)");

        var matches = nameRegex.Matches(titleCp);

        if(matches.Count() < 2){
            ErrorBuffer += title;
            return "";
        }
        else{
            var matchValue = matches[0].Value;
            titleCp = titleCp.Replace(matchValue, matchValue.Replace("(", "").Replace(")", ""));
        }

        string @unsafe = titleCp.Contains('*') ? "unsafe" : "";

        return 
        "\n" +
        $"[DllImport(lib)]\n"+
        $"public static extern {@unsafe}   {titleCp};\n";
    }

    private string TranslateFunctionTitles(ref string src)
    {
        StringBuilder result = new StringBuilder("");
        Regex functionRegex = new Regex(@"(?:unsigned\s+)*[\w_]+\s+[\w_]+\s*\([^{};()]*\)");
        foreach (var match in functionRegex.Matches(src))
        {
            string? title = match?.ToString();
            if(title != null)
                result.Append(TranslateFunctionTitle(title));
            
        }

        src = functionRegex.Replace(src, "");
        return result.ToString();
    }


    private string TranslateFunctionTitle(string title)
    {
        string titleCp = title.Replace("struct", "");

        string @unsafe = titleCp.Contains('*') ? "unsafe" : "";

        return 
        $"[DllImport(lib)]\n"+
        $"public static extern {@unsafe}   {titleCp};\n";
    }
}