using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.ConfigGenerators.Impl;

public class DotNetDatabaseConfigGenerator : IConfigGenerator
{
    public void GenerateConfig(string pathToConfigDirectory, string uuid)
    {
        GenerateConfigForFile(uuid,$"{pathToConfigDirectory}/appsettings.json");
        GenerateConfigForFile(uuid,$"{pathToConfigDirectory}/appsettings.Development.json");
    }

    private static void  GenerateConfigForFile(string uuid, string file)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var lines = new List<string>(File.ReadLines(file));
        lines.Insert(1,$@"  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{generatorVariables.DatabaseConnectionData.ToConnectionString()}""
  }},");
        File.WriteAllLines(file, lines);
    }
}