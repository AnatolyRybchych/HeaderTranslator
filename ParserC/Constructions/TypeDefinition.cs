
using HeaderTranslator.ParserC;
namespace HeaderTranslator.ParserC.Constructions;

public class TypeDefinition : IConstruction
{
    public string Source { get; private set; }

    public bool IsHasBody { get; private set; }

    public string Name { get; private set; }

    public Type? Pseudoname { get; private set; }

    public bool IsPseudonameDefinition => Pseudoname != null;

    public bool IsStructDefinition => Body != null;
    
    public Type DefinedType => new Type(Name);

    public Struct? Body { get; private set; }



    public TypeDefinition(string nameToDefine, Type defineFrom)
    {
        IsHasBody = false;

        Name = nameToDefine;
        Pseudoname = defineFrom;
        Source = $"typedef {defineFrom.ExtendedName} {Name}";
    }

    public TypeDefinition(string nameToDefine, Struct defineFrom)
    {
        IsHasBody = true;
        Name = nameToDefine.Trim();
        Body = defineFrom;
        Source = $"typedef {Body.SourceAsAnonymous} {Name}";
    }
}