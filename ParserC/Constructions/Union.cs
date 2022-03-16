
namespace HeaderTranslator.ParserC.Constructions;

public class Union : IConstruction
{
    public string Source { get; private set;}

    public bool IsHasBody => true;

    public string Name { get; private set; }

    public bool CSUnsafe { get; private set; }

    public Variable[] Fields { get; set; }

    public Union(string name, Variable[] fields)
    {
        Fields = fields;
        Name = name.Trim();
        Source = 
        $"union {Name}{{\n" +
        string.Join("", Fields.Select(field => $"\t{field.Source};\n")) + "}";
        
        CSUnsafe = Source.Contains('*');
    }
}