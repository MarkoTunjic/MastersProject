namespace GrpcGenerator.Generators.ConfigGenerators;

public interface IConfigGenerator
{
    public void GenerateConfig(string pathToConfigDirectory, string uuid);
}