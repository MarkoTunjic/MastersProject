namespace GrpcGenerator.Generators.RepositoryGenerators;

public interface IRepositoryGenerator
{
    public void GenerateRepositories(string uuid, string targetDirectory);
    public void GenerateRepository(string modelName, string[] primaryKeys, string targetDirectory);
    public string GetCreateMethodCode(string modelName);
    public string GetDeleteMethodCode(string modelName, string[] primaryKeys);
    public string GetReadAllMethodCode(string modelName);
    public string GetFindByIdMethodCode(string modelName, string[] primaryKeys);
    public string GetUpdateMethodCode(string modelName);
}