namespace GrpcGenerator.Generators.MapperGenerators;

public interface IMapperGenerator
{
    public void GenerateMappers(string pathToModels, string targetDirectory);
}