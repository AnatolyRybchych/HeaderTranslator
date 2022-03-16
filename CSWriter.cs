
using System.Text.RegularExpressions;
using HeaderTranslator.ParserC;
using HeaderTranslator.ParserC.Constructions;

namespace HeaderTranslator;

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
        $"public static class {OutClass}{{\n" +

        Tabulate(
            $"public const string LibPath = \"{InLibrary}\";" +
            WriteFunctions(parser.Functions) +
            WriteStructures(parser.Structures) + 
            WriteTypes(parser.Typedefs) +
            WriteUnions(parser.Unions)
        ) +
        $"}}";
    }

    private string WriteUnions(List<Union> unions)
    {
        return string.Join("", unions.Select(union => $"{WriteUnion(union)}\n\n"));
    }

    private object WriteUnion(Union union)
    {
        if(WasWriten(union) == false)
        {
            return 
            $"[StructLayout(LayoutKind.Explicit)] \n" +
            $"public unsafe struct {union.Name} {{{WriteVariables(union.Fields, "", (field, id) => $"\n    [FieldOffset(0)] {WriteVariable(field, id)};" )};\n}}";
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
                if(WasWriten(typedef) == false)
                {
                    res += $"public unsafe struct {WriteType(typedef.DefinedType)}{{{WriteType(typedef.Pseudoname) } val;}}\n\n";
                }
            }
        }

        return res;
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
            var res = $"public unsafe struct {structure.Name} {{{WriteVariables(structure.Args, "", (arg, id) => $"\n    {WriteVariable(arg, id)};" )}\n}}";
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
            return 
            $"[DllImport(LibPath)]\n" +
            $"public static unsafe extern {WriteType(func.ReturnType)} {func.Name} ({WriteVariables(func.Arguments, ",", ((arg , id)=> $" {WriteVariable(arg, id)}"))});";
        }
        else
        {
            return "";
        }
    }

    public string WriteVariables(Variable[] vars, string separator , Func<Variable, int, string>? eachVar)
    {
        return string.Join(separator, vars.Select((var, i) => eachVar?.Invoke(var, i)));
    }
    public string WriteVariable(Variable var, int number)
    {
        string fx = $"{(var.ArrayLength == 1?"":"fixed ")}";
        string arr = $"{(var.ArrayLength == 1?"":$"[{var.ArrayLength}]")}";
        return $"{fx}{WriteType(var.Type)} {var.Name ?? $"arg{number}"}";
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
            if(type.Unsigned) res += "u";
            if(type.Short) res += "short";
            else if(type.Long) res += "long";
            else  res += "int";
        }
        else if(type.Name == "char")
        {
            if(type.Unsigned) res += "byte";
            else res += "sbyte";
        }
        else if(type.Name == "short")
        {
            if(type.Unsigned) res += "ushort";
            else res += "short";
        }
        else if(type.Name == "long")
        {
            if(type.Unsigned) res += "ulong";
            else res += "long";
        }
        else if(type.Name == "char" || type.Name == "float"  || type.Name == "double" )
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
                return $"IntPtr /*|{type.Name}{new string('*', type.PointerVolume)}|*/ ";
            }
        }

        return res + (type.PointerVolume == 0 ? "" : new string('*', type.PointerVolume));
    }
}