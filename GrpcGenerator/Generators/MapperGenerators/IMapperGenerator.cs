namespace GrpcGenerator.Generators.MapperGenerators;

public interface IMapperGenerator
{
    public void GenerateMappers(string pathToDtos, string targetDirectory, string targetPackage, string modelsPackage,
        string dtoPackage);
}