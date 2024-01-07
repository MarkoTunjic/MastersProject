namespace GrpcGenerator.Generators.ModelGenerators;

public interface IModelGenerator
{
    public void GenerateModels(string databaseName, string databaseServer, string databasePort, string databaseUid,
        string databasePwd, string provider, string destinationFolder, string projectName);
}