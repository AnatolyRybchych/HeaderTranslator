
using HeaderTranslator.ParserC;
namespace HeaderTranslator.ParserC.Constructions;

public class Variable : IConstruction
{
    public string Source { get; private set; }

    public bool IsHasBody => false;

    public string? Name { get; private set; }

    public bool IsUnnamed => Name == null;

    public Type Type { get; private set; }

    public int ArrayLength { get; set;}



    //can be unnamed (old function style)
    public Variable(Type type, string? name = null, int arrayLengh = 1)
    {
        Name = name?.Trim();
        Name = string.IsNullOrWhiteSpace(Name) ? null : Name;
        this.Type = type;
        ArrayLength = arrayLengh; 
        string arrSufix = ArrayLength == 1 ? "" : $"[{ArrayLength}]";
        Source = $"{Type.ExtendedName} {Name ?? ""}{arrSufix}";

    }

    //if unnamed returns with name $"arg{argNum}"
    public string GetNamedSource(int argNum) => $"{Type.ExtendedName} {Name ?? $"arg{argNum}"}";
}