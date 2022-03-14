
namespace HeaderTranslator;
using System;

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

        HeaderTranslator translator = new HeaderTranslator(gcc.Result, Path.GetFileNameWithoutExtension(args[0]));
        System.Console.WriteLine(translator.Translate());
        
    }
}
