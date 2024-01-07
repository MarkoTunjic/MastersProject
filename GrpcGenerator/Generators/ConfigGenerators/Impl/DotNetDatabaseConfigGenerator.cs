namespace GrpcGenerator.Generators.ConfigGenerators.Impl;

public class DotNetDatabaseConfigGenerator : IConfigGenerator
{
    public void GenerateConfig(string pathToConfigDirectory, string databaseName, string databaseServer,
        string databasePort, string databaseUid, string databasePwd)
    {
        var lines = new List<string>(File.ReadLines($"{pathToConfigDirectory}/appsettings.json"));
        lines.Insert(1,
            $"  \"DefaultConnection\": \"Server={databaseServer};Port={databasePort};Database={databaseName};Uid={databaseUid};Pwd={databasePwd}\",");
        File.WriteAllLines($"{pathToConfigDirectory}/appsettings.json", lines);
    }
}