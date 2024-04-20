using GrpcGenerator.Domain;

namespace GrpcGenerator.Generators.RepositoryGenerators;

public interface IRepositoryGenerator
{
    public void GenerateRepositories(string uuid);

    public void GenerateRepository(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys,
        string targetDirectory);

    public string GetCreateMethodCode(string modelName);
    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys);
    public string GetFindAllMethodCode(string modelName);
    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys);
    public string GetUpdateMethodCode(string modelName);

    public string GetFindByForeignKeyMethodCode(string modelName,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys);
}