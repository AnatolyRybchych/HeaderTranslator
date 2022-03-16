
namespace HeaderTranslator.ParserC;

public interface IConstruction
{
    string Source { get; }
    bool IsHasBody { get; }
    string? Name { get; }
}