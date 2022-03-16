

using System;
using System.Text;
using HeaderTranslator.ParserC;

namespace HeaderTranslator;

class Program{
    static void Main(string[] args){
        if(args.Length == 0){
            string? currentFile = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            if(currentFile == null) currentFile = "HeaderTranslatoe";

            Console.WriteLine($"Error required arguments");
            Console.WriteLine($"Usage:");
            Console.WriteLine($"{currentFile} headerFile.h [-g++]");
            Console.WriteLine($"[args] -> args is variadic");
            return;
        }

        bool useGpp = args.Where(
            (arg, id) => id != 0 && arg.Trim().ToLowerInvariant() == "-g++"
        ).Count() != 0;

        Gcc.Compiller compiller = useGpp ? Gcc.Compiller.Gpp : Gcc.Compiller.GCC;
        string file = args[0];
        Gcc gcc = new Gcc();

        gcc.Preprocess(file, compiller);
        
        ParserC.Parser parser= new ParserC.Parser(gcc.Result);
        parser.Parse();

        CSWriter writer = new CSWriter();
        Console.WriteLine(writer.Write(parser));;

        Console.WriteLine(
            "/* errors:\n"+
            string.Join("\n    ", writer.NotWritedConstructions
            .Where(ctruct => ctruct.Name != null)
            .Select(ctruct => $"construction:{{{ctruct.Name}}}, value_c:{{{ctruct.Construction.Source}}}"))+
            "*/"
        );
    } 
}
