using GrpcGenerator.Domain;

namespace GrpcGenerator.Generators.ServiceGenerators;

public interface IServiceGenerator
{
    public void GenerateServices(string uuid);

    public void GenerateService(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys,
        string targetDirectory);

    public string GetCreateMethodCode(string modelName, Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys);
    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys);
    public string GetFindAllMethodCode(string modelName);
    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys);
    public string GetUpdateMethodCode(string modelName, Dictionary<string, Type> primaryKeys);

    public string GetFindByForeignKeyMethods(string modelName,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys);
}