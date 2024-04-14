using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.MapperGenerators.Impl;

public class DotNetGrpcMapperGenerator : IMapperGenerator
{
    public void GenerateMappers(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Domain/Mappers";
        Directory.CreateDirectory(targetDirectory);
        var pathToDtos = $"{generatorVariables.ProjectDirectory}/Domain/Dto";

        using var stream = new StreamWriter(File.Create($"{targetDirectory}/GrpcMapperRegistration.cs"));

        stream.WriteLine($"using {generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace};");
        stream.WriteLine($"using {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};");
        stream.WriteLine("using AutoMapper;");
        stream.WriteLine($"\nnamespace {generatorVariables.ProjectName}.{NamespaceNames.MappersNamespace};");
        stream.WriteLine("public class GrpcMapperRegistration : Profile \n{");
        stream.WriteLine("\tpublic GrpcMapperRegistration() \n\t{");

        foreach (var file in Directory.EnumerateFiles(pathToDtos))
        {
            var dtoName =
                file[
                    (file.LastIndexOf("/", StringComparison.Ordinal) + 1)..file.LastIndexOf(".",
                        StringComparison.Ordinal)];
            var modelName = dtoName[..dtoName.LastIndexOf("Dto", StringComparison.Ordinal)];
            stream.WriteLine($"\t\tCreateMap<{modelName}Dto, {modelName}Reply>();");
            stream.WriteLine($"\t\tCreateMap<{modelName}UpdateRequest, {modelName}WriteDto>();");
            stream.WriteLine($"\t\tCreateMap<{modelName}CreateRequest, {modelName}WriteDto>();");
        }

        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}