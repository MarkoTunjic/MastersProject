using System.Diagnostics;
using GrpcGenerator.Domain;

namespace GrpcGenerator.Generators.ModelGenerators.Impl;

public class EfCoreModelGenerator : IModelGenerator
{
    private static readonly Dictionary<DotNetSupportedDBMS, string> _addServiceCommand = new()
    {
        {
            DotNetSupportedDBMS.PostgreSql,
            "options.UseNpgsql(configuration.GetConnectionString(\"DefaultConnection\"));"
        }
    };

    private static readonly Dictionary<string, DotNetSupportedDBMS> _stringToEnum = new()
    {
        {
            "postgres",
            DotNetSupportedDBMS.PostgreSql
        }
    };

    public void GenerateModels(string databaseName, string databaseServer, string databasePort, string databaseUid,
        string databasePwd, string provider, string destinationFolder, string projectName)
    {
        var process = new Process();
        process.StartInfo.WorkingDirectory = destinationFolder;
        process.StartInfo.FileName = "efcpt";
        var connectionString =
            $"Server={databaseServer};Port={databasePort};Database={databaseName};Uid={databaseUid};Pwd={databasePwd};";
        process.StartInfo.Arguments = $"\"{connectionString}\" {provider}";
        process.Start();
        process.WaitForExit();
        GenerateModelsRegistration(destinationFolder, databaseName, provider, projectName);
    }

    private static void GenerateModelsRegistration(string targetDirectory, string databaseName, string provider,
        string projectName)
    {
        using var stream = new StreamWriter(File.Create($"{targetDirectory}/ModelServiceRegistration.cs"));

        stream.WriteLine("using Microsoft.EntityFrameworkCore;");
        stream.WriteLine("using Domain.Models;\n");
        stream.WriteLine($"namespace {projectName}.Domain;");
        stream.WriteLine("public static class ModelServiceRegistration\n{");
        stream.WriteLine(
            "\tpublic static void AddModels(this IServiceCollection services, IConfiguration configuration)\n\t{");
        stream.WriteLine($"\t\tservices.AddDbContext<{databaseName}Context>(options =>\n\t\t{{");
        stream.WriteLine($"\t\t\t{_addServiceCommand[_stringToEnum[provider]]}");
        stream.WriteLine("\t\t}, contextLifetime: ServiceLifetime.Transient);");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}