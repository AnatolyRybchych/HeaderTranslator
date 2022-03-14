
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HeaderTranslator;

public class Gcc{
    public string ConsoleLog { get; private set; } = "";
    public string Result{ get; private set; } = "";
    public void Preprocess(string file, Compiller compiller = Compiller.GCC){
        string compillerPath;

        if(compiller == Compiller.GCC) compillerPath = "gcc";
        else if(compiller == Compiller.Gpp) compillerPath = "g++";
        else throw new NotImplementedException();

        ExecuteCommand($"{compillerPath} -E {file}");
    }

    public enum Compiller{
        GCC,
        Gpp
    }

    private void ExecuteCommand(string command)
    {
        Process p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        string tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, $"#/bin/bash\n{command}");
        p.StartInfo.Arguments = $"{tmp}";
        p.StartInfo.FileName = "bash";
        p.Start();
        Result =  p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        File.Delete(tmp);
    }

}