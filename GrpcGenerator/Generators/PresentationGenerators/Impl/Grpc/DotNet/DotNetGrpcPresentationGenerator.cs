using GrpcGenerator.Generators.MapperGenerators.Impl;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.Grpc.DotNet;

public class DotNetGrpcPresentationGenerator : IPresentationGenerator
{
    public void GeneratePresentation(string uuid)
    {
        new GrpcProtofileGenerator().GeneratePresentation(uuid);
        new DotNetGrpcMapperGenerator().GenerateMappers(uuid);
        new DotNetGrpcServicesGenerator().GeneratePresentation(uuid);
        GenerateServiceRegistration(uuid);
        GenerateWebAppRegistration(uuid);
    }

    private static void GenerateServiceRegistration(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        using var stream =
            new StreamWriter(
                File.Create(
                    $"{generatorVariables.ProjectDirectory}/Presentation/GrpcPresentationServiceRegistration.cs"));
        stream.Write($@"namespace {generatorVariables.ProjectName}.Presentation;
public static class GrpcPresentationServiceRegistration
{{
    public static void AddGrpcPresentation(this IServiceCollection services)
    {{
        services.AddGrpc();
    }}
}}");
    }
    
    
    private static void GenerateWebAppRegistration(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var pathToModels = $"{generatorVariables.ProjectDirectory}/Domain/Models";
        var serviceRegistration = Directory.EnumerateFiles(pathToModels)
            .Select(modelName => modelName.Replace(".cs", ""))
            .Select(modelName=>modelName[(modelName.LastIndexOf("/", StringComparison.Ordinal)+1)..])
            .Where(modelName => modelName != $"{generatorVariables.DatabaseConnectionData.DatabaseName}Context")
            .Aggregate("", (current, className) => current + $"\t\tapp.MapGrpcService<Grpc{className}ServiceImpl>();\n");

        serviceRegistration = serviceRegistration[..^1];
        using var stream =
            new StreamWriter(
                File.Create(
                    $"{generatorVariables.ProjectDirectory}/Presentation/GrpcPresentationWebAppRegistration.cs"));
        stream.Write($@"using {generatorVariables.ProjectName}.Presentation.Grpc;

namespace {generatorVariables.ProjectName}.Presentation;
public static class GrpcPresentationWebAppRegistration
{{
    public static void AddGrpcPresentation(this WebApplication app)
    {{
{serviceRegistration}
    }}
}}");
    }
}