using System.Reflection;

#pragma warning disable RS1035 // Do not use APIs banned for analyzers

namespace Generator;

public class OutputWriter
{
    public static string GetAssemblyLocation() => Assembly.GetExecutingAssembly().Location;

    public static void WriteFileOnProjectExe(string output, string fileName)
    {
        var path = Directory.GetParent(GetAssemblyLocation()).FullName;
        using var writter = new StreamWriter(path + '\\' + fileName);
        writter.Write(output);
    }

    public static void WriteFileOnProject(string output, string fileName)
    {
        var path = Directory.GetParent(GetAssemblyLocation()).Parent.Parent.Parent.FullName;
        using var writter = new StreamWriter(path + '\\' + fileName);
        writter.Write(output);
    }
}