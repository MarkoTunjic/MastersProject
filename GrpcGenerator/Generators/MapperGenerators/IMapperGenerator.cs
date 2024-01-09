namespace GrpcGenerator.Generators.MapperGenerators;

public interface IMapperGenerator
{
    public void GenerateMappers(string uuid, string targetPackage, string modelsPackage, string dtoPackage);
}