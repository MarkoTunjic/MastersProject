using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.DotNet;

public class DotNetGrpcPresentationGenerator : IPresentationGenerator
{
    public void GeneratePresentation(string uuid)
    {
        GenerateProtofile(uuid);
    }

    private static void GenerateProtofile(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectName}/Protos");
        using var stream = new StreamWriter(File.Create($"{generatorVariables.ProjectName}/Protos/protofile.proto"));
    }
}