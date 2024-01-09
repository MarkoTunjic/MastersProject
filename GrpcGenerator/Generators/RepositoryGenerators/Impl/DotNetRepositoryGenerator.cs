using System.Data;
using GrpcGenerator.Utils;
using Npgsql;

namespace GrpcGenerator.Generators.RepositoryGenerators.Impl;

public class DotNetRepositoryGenerator : IRepositoryGenerator
{
    public void GenerateRepositories(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Infrastructure/Repositories";
        Directory.CreateDirectory(targetDirectory);
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
                .ToDictionary(c => GetDotnetNameFromSqlName(c.ColumnName), c => c.DataType)!;
            var modelName = GetDotnetNameFromSqlName(tableName);
            GenerateRepository(uuid, modelName, primaryKeys, targetDirectory);
        }

        conn.Close();
    }

    public void GenerateRepository(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        string targetDirectory)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var createMethod = GetCreateMethodCode(modelName);
        var deleteMethod = GetDeleteMethodCode(modelName, primaryKeys);
        var readAllMethod = GetFindAllMethodCode(modelName);
        var findById = GetFindByIdMethodCode(modelName, primaryKeys);
        var updateMethod = GetUpdateMethodCode(modelName, primaryKeys);
        using var interfaceStream = new StreamWriter(File.Create($"{targetDirectory}/I{modelName}Repository.cs"));
        interfaceStream.Write($@"using Domain.Models;

namespace {generatorVariables.ProjectName}.Infrastructure.Repositories;
public interface I{modelName}Repository
{{
    {createMethod[..createMethod.IndexOf("\n", StringComparison.Ordinal)]};
    {deleteMethod[..deleteMethod.IndexOf("\n", StringComparison.Ordinal)]};
    {readAllMethod[..readAllMethod.IndexOf("\n", StringComparison.Ordinal)]};
    {findById[..findById.IndexOf("\n", StringComparison.Ordinal)]};
    {updateMethod[..updateMethod.IndexOf("\n", StringComparison.Ordinal)]};
}}
");
        using var classStream = new StreamWriter(File.Create($"{targetDirectory}/Impl/{modelName}Repository.cs"));
        classStream.Write($@"using Domain.Models;

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
        return $@"public {modelName} Create{modelName}({modelName} new{modelName})
    {{
        _dbContext.Add(new{modelName});
        _dbContext.SaveChanges();
        return new{modelName};
    }}";
    }

    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return $@"public void Delete{modelName}ById({GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        var tbd = Find{modelName}ById({GetMethodInputForPrimaryKeys(primaryKeys, true)});
        if(tbd == null)
        {{
            return;
        }}
        _dbContext.{modelName}s.Remove(tbd);
        _dbContext.SaveChanges();
    }}";
    }

    public string GetFindAllMethodCode(string modelName)
    {
        return $@"public List<{modelName}> FindAll{modelName}()
    {{
        return _dbContext.{modelName}s.ToList();
    }}";
    }

    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return $@"public {modelName}? Find{modelName}ById({GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        return _dbContext.{modelName}s.SingleOrDefault(x => {GetPrimaryKeyQuery(primaryKeys, "", true)});
    }}";
    }

    public string GetUpdateMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return $@"public void Update{modelName}({modelName} updated{modelName})
    {{
        _dbContext.Entry(_dbContext.{modelName}s.FirstOrDefault(x => {GetPrimaryKeyQuery(primaryKeys, $"updated{modelName}", false)})).CurrentValues.SetValues(updated{modelName});
        _dbContext.SaveChanges();
    }}
";
    }


    private static string GetDotnetNameFromSqlName(string sqlName)
    {
        var result = "";
        foreach (var part in sqlName.Split("_"))
        {
            var firstLetter = char.ToUpper(part[0]);
            result += firstLetter + part[1..];
        }

        if (char.ToLower(result[^1]) == 's') result = result[..^1];
        return result;
    }

    private static string GetPrimaryKeyQuery(IReadOnlyDictionary<string, Type> primaryKeys, string inputPrefix,
        bool lower)
    {
        var primaryKeyQuery = "";
        var i = 0;
        foreach (var entry in primaryKeys)
        {
            if (i != 0) primaryKeyQuery += " && ";
            primaryKeyQuery +=
                $"x.{entry.Key} == {inputPrefix}{(lower ? $"{char.ToLower(entry.Key[0])}{entry.Key[1..]}" : $".{entry.Key}")}";
            i++;
        }

        return primaryKeyQuery;
    }

    private static string GetMethodInputForPrimaryKeys(IReadOnlyDictionary<string, Type> primaryKeys, bool call)
    {
        var result = "";
        var i = 0;
        foreach (var entry in primaryKeys)
        {
            if (i != 0) result += ", ";
            result += $"{(call ? "" : entry.Value + " ")}{char.ToLower(entry.Key[0]) + entry.Key[1..]}";
            i++;
        }

        return result;
    }

    private static string GetMethodCallForPrimaryKeys(IReadOnlyDictionary<string, Type> primaryKeys)
    {
        var result = "";
        var i = 0;
        foreach (var entry in primaryKeys)
        {
            if (i != 0) result += ", ";
            result += $"{char.ToLower(entry.Key[0]) + entry.Key[1..]}";
            i++;
        }

        return result;
    }
}