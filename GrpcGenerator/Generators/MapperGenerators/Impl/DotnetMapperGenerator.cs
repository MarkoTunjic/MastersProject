namespace GrpcGenerator.Generators.MapperGenerators.Impl;

public class DotnetMapperGenerator : IMapperGenerator
{
    public void GenerateMappers(string pathToDtos, string targetDirectory, string targetPackage, string modelsPackage,
        string dtoPackage)
    {
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
    }
}