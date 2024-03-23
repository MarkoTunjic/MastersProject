using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.DotNet;

public class DotNetGrpcPresentationGenerator : IPresentationGenerator
{
    private static readonly Dictionary<Type, string> DotNetToGrpcType = new()
    {
        { typeof(int), "int32" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(long), "int64" },
        { typeof(uint), "uint32" },
        { typeof(ulong), "uint64" },
        { typeof(bool), "bool" },
        { typeof(string), "string" }
    };

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