using System.Diagnostics;
using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.ModelGenerators.Impl;

public class EfCoreModelGenerator : IModelGenerator
{
    private static readonly Dictionary<DotNetSupportedDBMS, string> AddServiceCommand = new()
    {
        {
            DotNetSupportedDBMS.PostgreSql,
            "options.UseNpgsql(configuration.GetConnectionString(\"DefaultConnection\"));"
        }
    };

    private static readonly Dictionary<string, DotNetSupportedDBMS> StringToEnum = new()
    {
        {
            "postgres",
            DotNetSupportedDBMS.PostgreSql
        }
    };

    public void GenerateModels(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var destinationFolder = $"{generatorVariables.ProjectDirectory}/Domain";
        Directory.CreateDirectory(destinationFolder);
        var process = new Process();
        process.StartInfo.WorkingDirectory = destinationFolder;
        process.StartInfo.FileName = "efcpt";
        var connectionString = generatorVariables.DatabaseConnection.ToConnectionString();
            process.StartInfo.Arguments = $"\"{connectionString}\" {generatorVariables.DatabaseConnection.Provider}";
        process.Start();
        process.WaitForExit();
        GenerateModelsRegistration(destinationFolder, generatorVariables.DatabaseConnection.DatabaseName,
            generatorVariables.DatabaseConnection.Provider, generatorVariables.ProjectName);
    }

    private static void GenerateModelsRegistration(string targetDirectory, string databaseName, string provider,
        string projectName)
    {
        using var stream = new StreamWriter(File.Create($"{targetDirectory}/ModelServiceRegistration.cs"));

        stream.WriteLine("using Microsoft.EntityFrameworkCore;");
        stream.WriteLine($"using {NamespaceNames.ModelsNamespace};\n");
        stream.WriteLine($"namespace {projectName}.Domain;");
        stream.WriteLine("public static class ModelServiceRegistration\n{");
        stream.WriteLine(
            "\tpublic static void AddModels(this IServiceCollection services, IConfiguration configuration)\n\t{");
        stream.WriteLine($"\t\tservices.AddDbContext<{databaseName}Context>(options =>\n\t\t{{");
        stream.WriteLine($"\t\t\t{AddServiceCommand[StringToEnum[provider]]}");
        stream.WriteLine("\t\t}, contextLifetime: ServiceLifetime.Transient);");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}