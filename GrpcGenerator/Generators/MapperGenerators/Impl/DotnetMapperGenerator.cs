using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.MapperGenerators.Impl;

public class DotnetMapperGenerator : IMapperGenerator
{
    public void GenerateMappers(string uuid, string targetPackage, string modelsPackage, string dtoPackage)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Domain/Mappers";
        Directory.CreateDirectory(targetDirectory);
        var pathToDtos = $"{generatorVariables.ProjectDirectory}/Domain/Dto";

        using var stream = new StreamWriter(File.Create($"{targetDirectory}/MapperRegistration.cs"));

        stream.WriteLine($"using {modelsPackage};");
        stream.WriteLine($"using {dtoPackage};");
        stream.WriteLine("using AutoMapper;");
        stream.WriteLine($"\nnamespace {targetPackage};");
        stream.WriteLine("public class MapperRegistration : Profile \n{");
        stream.WriteLine("\tpublic MapperRegistration() \n\t{");

        foreach (var file in Directory.EnumerateFiles(pathToDtos))
        {
            var dtoName =
                file[
                    (file.LastIndexOf("/", StringComparison.Ordinal) + 1)..file.LastIndexOf(".",
                        StringComparison.Ordinal)];
            var modelName = dtoName[..dtoName.LastIndexOf("Dto", StringComparison.Ordinal)];
            stream.WriteLine($"\t\tCreateMap<{modelName},{dtoName}>();");
        }

        stream.WriteLine("\t}");
        stream.WriteLine("}");
        GenerateMapperServiceRegistration(targetDirectory, targetPackage);
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