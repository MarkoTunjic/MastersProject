using System.Diagnostics;

namespace GrpcGenerator.generators.impl;

public class EfCoreModelGenerator : IModelGenerator
{
    public void GenerateModels(string connectionString, string provider, string destinationFolder)
    {
        var process = new Process();
        process.StartInfo.WorkingDirectory = destinationFolder;
        process.StartInfo.FileName = "efcpt";
        process.StartInfo.Arguments = $"\"{connectionString}\" {provider}";
        process.Start();
        process.WaitForExit();
    }
}