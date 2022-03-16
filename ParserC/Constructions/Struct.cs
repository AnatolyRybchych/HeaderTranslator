
using HeaderTranslator.ParserC;
namespace HeaderTranslator.ParserC.Constructions;

public class Struct : IConstruction
{
    public string Source { get; private set; }

    public bool IsHasBody => true;

    public string? Name { get; set; }

    public bool IsAnomymous => Name == null;

    public bool IsWithoutFields => Args.Length == 0;

    public Variable[] Args { get; private set; }  

    public string SourceAsAnonymous => 
        $"{Name ?? ""}{{\n" +
         string.Join("", Args.Select(arg => $"\t{arg.Type.ExtendedName} {arg.Name};\n")) +
        "}";

    public Struct(Variable[] args, string? name = null)
    {
        Args = args;
        Name = name?.Trim();
        Name = string.IsNullOrEmpty(Name) ? null : Name;
        Source = $"struct {(name ?? "")} {SourceAsAnonymous}";
    }
}
