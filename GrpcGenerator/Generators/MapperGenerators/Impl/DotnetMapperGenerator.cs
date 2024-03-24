using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.MapperGenerators.Impl;

public class DotnetMapperGenerator : IMapperGenerator
{
    public void GenerateMappers(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Domain/Mappers";
        Directory.CreateDirectory(targetDirectory);
        var pathToDtos = $"{generatorVariables.ProjectDirectory}/Domain/Dto";

        using var stream = new StreamWriter(File.Create($"{targetDirectory}/MapperRegistration.cs"));

        stream.WriteLine($"using {NamespaceNames.ModelsNamespace};");
        stream.WriteLine($"using {generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace};");
        stream.WriteLine($"using {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};");
        stream.WriteLine("using AutoMapper;");
        stream.WriteLine($"\nnamespace {generatorVariables.ProjectName}.{NamespaceNames.MappersNamespace};");
        stream.WriteLine("public class MapperRegistration : Profile \n{");
        stream.WriteLine("\tpublic MapperRegistration() \n\t{");

        foreach (var file in Directory.EnumerateFiles(pathToDtos))
        {
            var dtoName =
                file[
                    (file.LastIndexOf("/", StringComparison.Ordinal) + 1)..file.LastIndexOf(".",
                        StringComparison.Ordinal)];
            var modelName = dtoName[..dtoName.LastIndexOf("Dto", StringComparison.Ordinal)];
            stream.WriteLine($"\t\tCreateMap<{modelName}, {dtoName}>();");
            stream.WriteLine($"\t\tCreateMap<{dtoName}, {modelName}>();");
            stream.WriteLine($"\t\tCreateMap<{modelName}, {modelName}WriteDto>();");
            stream.WriteLine($"\t\tCreateMap<{modelName}WriteDto, {modelName}>();");
        }

        stream.WriteLine("\t}");
        stream.WriteLine("}");
        GenerateMapperServiceRegistration(targetDirectory,
            $"{generatorVariables.ProjectName}.{NamespaceNames.MappersNamespace}");
    }

    private static void GenerateMapperServiceRegistration(string targetDirectory, string targetPackage)
    {
        using var stream = new StreamWriter(File.Create($"{targetDirectory}/MapperServiceRegistration.cs"));

        stream.WriteLine("using Microsoft.Extensions.DependencyInjection;\n");
        stream.WriteLine($"namespace {targetPackage};");
        stream.WriteLine("public static class MapperServiceRegistration\n{\n");
        stream.WriteLine("\tpublic static void AddMappers(this IServiceCollection services)\n\t{\n");
        stream.WriteLine("\t\tservices.AddAutoMapper(typeof(MapperServiceRegistration));");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}