namespace GrpcGenerator.Generators.DependencyGenerators;

public interface IDependencyGenerator
{
    public void GenerateDependencies(string uuid, string pathToDependencyFile);
}