
namespace HeaderTranslator.ParserC;

public class Type
{
    public string Name {get; private set; }

    public bool Short { get; private set; }

    public bool Long { get; private set; }

    public bool Signed { get; private set; }

    public bool Unsigned { get; private set; }

    public int PointerVolume { get; set;}

    public string ExtendedName => 
    $"{(Unsigned?"unsigned ":"")}{(Signed?"signed ":"")}"+
    $"{(Short?"short ":"")}{(Long?"long ":"")}"+
    $"{Name}{new string('*',PointerVolume)}";


    public Type(string name, int pointerVolume = 0, bool unsigned = false, bool signed = false, bool @short = false, bool @long = false)
    {
        Name = name.Trim();

        PointerVolume = pointerVolume;
        Signed = signed;
        Unsigned = unsigned;
        Short = @short;
        Long = @long;
    }
}