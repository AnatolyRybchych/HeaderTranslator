
namespace HeaderTranslator.ParserC.Constructions;

public class Function: IConstruction
{
    public string Source { get; private set; }

    public bool IsHasBody => false;

    public string Name { get; private set;}

    public Variable[] Arguments { get; private set; }


    public Type ReturnType { get; private set;}
    public Function(Type returnType, string name, Variable[] args)
    {
        Arguments = args.Where(arg => !((arg.Name == "void") && (arg.Type.PointerVolume == 0))).ToArray();
        
        Name = name.Trim();
        ReturnType = returnType;
        Source = $"{ReturnType.ExtendedName} {Name} ({string.Join(',', Arguments.Select((arg, id) => arg.GetNamedSource(id)))})";
    }
}