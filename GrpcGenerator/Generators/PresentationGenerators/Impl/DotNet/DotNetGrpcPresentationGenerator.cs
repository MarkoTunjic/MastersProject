using GrpcGenerator.Generators.MapperGenerators.Impl;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.DotNet;

public class DotNetGrpcPresentationGenerator : IPresentationGenerator
{
    public void GeneratePresentation(string uuid)
    {
        new GrpcProtofileGenerator().GeneratePresentation(uuid);
        new DotNetGrpcMapperGenerator().GenerateMappers(uuid);
        new DotNetGrpcServicesGenerator().GeneratePresentation(uuid);
        GeneratePresentationServiceRegistration(uuid);
    }

    private static void GeneratePresentationServiceRegistration(string uuid)
    {
    }
}