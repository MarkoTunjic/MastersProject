namespace GrpcGenerator.Generators.DtoGenerators;

public interface IDtoGenerator
{
    public void GenerateDtos(string pathToModels, string destinationDirectory, string databaseName, string packageName);
}