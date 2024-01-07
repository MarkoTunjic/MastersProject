namespace GrpcGenerator.Generators.ConfigGenerators;

public interface IConfigGenerator
{
    public void GenerateConfig(string pathToConfigDirectory, string databaseName, string databaseServer,
        string databasePort, string databaseUid,
        string databasePwd);
}