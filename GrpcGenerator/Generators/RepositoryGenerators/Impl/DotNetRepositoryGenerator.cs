using System.Data;
using GrpcGenerator.Utils;
using Npgsql;

namespace GrpcGenerator.Generators.RepositoryGenerators.Impl;

public class DotNetRepositoryGenerator : IRepositoryGenerator
{
    public void GenerateRepositories(string uuid, string targetDirectory)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var conn = new NpgsqlConnection(
            $"Server={generatorVariables.DatabaseConnection.DatabaseServer};Port={generatorVariables.DatabaseConnection.DatabasePort};Database={generatorVariables.DatabaseConnection.DatabaseName};Uid={generatorVariables.DatabaseConnection.DatabaseUid};Pwd={generatorVariables.DatabaseConnection.DatabasePwd};");
        Directory.CreateDirectory($"{targetDirectory}/Impl");
        conn.Open();
        var allTablesSchemaTable = conn.GetSchema("Tables");
        foreach (DataRow tableInfo in allTablesSchemaTable.Rows)
        {
            var tableName = (string)tableInfo.ItemArray[2]!;
            using var adapter = new NpgsqlDataAdapter("select * from " + tableName, conn);
            using var table = new DataTable(tableName);
            var primaryKeys = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .Select(c => GetDotnetNameFromSqlName(c.ColumnName))
                .ToArray()!;
            var modelName = GetDotnetNameFromSqlName(tableName);
            GenerateRepository(uuid, modelName, primaryKeys, targetDirectory);
        }

        conn.Close();
    }

    public void GenerateRepository(string uuid, string modelName, string[] primaryKeys, string targetDirectory)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var createMethod = GetCreateMethodCode(modelName);
        var deleteMethod = GetDeleteMethodCode(modelName, primaryKeys);
        var readAllMethod = GetReadAllMethodCode(modelName);
        var findById = GetFindByIdMethodCode(modelName, primaryKeys);
        var updateMethod = GetUpdateMethodCode(modelName);
        using var interfaceStream = new StreamWriter(File.Create($"{targetDirectory}/I{modelName}Repository.cs"));
        interfaceStream.Write($@"
using Domain.Models;

namespace {generatorVariables.ProjectName}.Infrastructure.Repositories;
public interface I{modelName}Repository
{{
    {createMethod[..createMethod.IndexOf("\n", StringComparison.Ordinal)]};
    {deleteMethod[..deleteMethod.IndexOf("", StringComparison.Ordinal)]};
    {readAllMethod[..readAllMethod.IndexOf("", StringComparison.Ordinal)]};
    {findById[..findById.IndexOf("", StringComparison.Ordinal)]};
    {updateMethod[..updateMethod.IndexOf("", StringComparison.Ordinal)]};
}}
");
        using var classStream = new StreamWriter(File.Create($"{targetDirectory}/Impl/{modelName}Repository.cs"));
        classStream.Write($@"
namespace {generatorVariables.ProjectName}.Infrastructure.Repositories.Impl;
public class {modelName}Repository : I{modelName}Repository
{{
    private readonly {generatorVariables.DatabaseConnection.DatabaseName}Context _dbContext;
    
    public {modelName}Repository({generatorVariables.DatabaseConnection.DatabaseName}Context dbContext)
    {{
        this._dbContext = dbContext;
    }}
    
    {createMethod}
    {deleteMethod}
    {readAllMethod}
    {findById}
    {updateMethod}
}}
");
    }

    public string GetCreateMethodCode(string modelName)
    {
        return $@"public List<{modelName}> Create{modelName}({modelName} new{modelName})
    {{
        return _dbContext.{modelName}.ToList();
    }}
";
    }

    public string GetDeleteMethodCode(string modelName, string[] primaryKeys)
    {
        return "";
    }

    public string GetReadAllMethodCode(string modelName)
    {
        return "";
    }

    public string GetFindByIdMethodCode(string modelName, string[] primaryKeys)
    {
        return "";
    }

    public string GetUpdateMethodCode(string modelName)
    {
        return "";
    }


    private static string GetDotnetNameFromSqlName(string sqlName)
    {
        var result = "";
        foreach (var part in sqlName.Split("_"))
        {
            var firstLetter = char.ToUpper(part[0]);
            result += firstLetter + part[1..];
        }

        return result;
    }
}