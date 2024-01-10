namespace GrpcGenerator.Generators.RepositoryGenerators;

public interface IRepositoryGenerator
{
    public void GenerateRepositories(string uuid);

    public void GenerateRepository(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        string targetDirectory);

    public string GetCreateMethodCode(string modelName);
    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys);
    public string GetFindAllMethodCode(string modelName);
    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys);
    public string GetUpdateMethodCode(string modelName);
}