using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.ConfigGenerators.Impl;

public class DotNetDatabaseConfigGenerator : IConfigGenerator
{
    public void GenerateConfig(string pathToConfigDirectory, string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var lines = new List<string>(File.ReadLines($"{pathToConfigDirectory}/appsettings.json"));
        lines.Insert(1,
            $"  \"DefaultConnection\": \"Server={generatorVariables.DatabaseConnection.DatabaseServer};Port={generatorVariables.DatabaseConnection.DatabasePort};Database={generatorVariables.DatabaseConnection.DatabaseName};Uid={generatorVariables.DatabaseConnection.DatabaseUid};Pwd={generatorVariables.DatabaseConnection.DatabasePwd}\",");
        File.WriteAllLines($"{pathToConfigDirectory}/appsettings.json", lines);
    }
}