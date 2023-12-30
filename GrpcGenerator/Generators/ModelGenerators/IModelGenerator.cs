namespace GrpcGenerator.Generators.ModelGenerators;

public interface IModelGenerator
{
    public void GenerateModels(string connectionString, string provider, string destinationFolder);
}