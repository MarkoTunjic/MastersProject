using System.Data;
using GrpcGenerator.Domain;
using GrpcGenerator.Utils;
using Npgsql;

namespace GrpcGenerator.Generators.RepositoryGenerators.Impl;

public class DotNetRepositoryGenerator : IRepositoryGenerator
{
    public void GenerateRepositories(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Infrastructure/Repositories";
        var conn = new NpgsqlConnection(
            $"Server={generatorVariables.DatabaseConnection.DatabaseServer};Port={generatorVariables.DatabaseConnection.DatabasePort};Database={generatorVariables.DatabaseConnection.DatabaseName};Uid={generatorVariables.DatabaseConnection.DatabaseUid};Pwd={generatorVariables.DatabaseConnection.DatabasePwd};");
        Directory.CreateDirectory($"{targetDirectory}/Impl");
        var models = Directory.GetFiles($"{generatorVariables.ProjectDirectory}/Domain/Models");
        var crudOperations = File.ReadAllText($"{targetDirectory}/Common/CrudOperations.cs");
        crudOperations = crudOperations.Replace("{ProjectName}", generatorVariables.ProjectName);
        File.WriteAllText($"{targetDirectory}/Common/CrudOperations.cs", crudOperations);
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
            if (models.Any(file => file.EndsWith($"{modelName}.cs")))
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
        interfaceStream.Write($@"using {NamespaceNames.ModelsNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace};
public interface I{modelName}Repository
{{
    {createMethod[..createMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {deleteMethod[..deleteMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {readAllMethod[..readAllMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {findById[..findById.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {updateMethod[..updateMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
}}
");
        using var classStream = new StreamWriter(File.Create($"{targetDirectory}/Impl/{modelName}Repository.cs"));
        classStream.Write($@"using {NamespaceNames.ModelsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace}.Common;

namespace {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace}.Impl;
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
        return $@"public async Task<{modelName}> CreateAsync{modelName}({modelName} new{modelName})
    {{
        return await CrudOperations.CreateAsync(new{modelName}, _dbContext);
    }}";
    }

    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return $@"public async Task Delete{modelName}ByIdAsync({GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        var tbd = await Find{modelName}ByIdAsync({GetMethodInputForPrimaryKeys(primaryKeys, true)});
        if(tbd == null)
        {{
            return;
        }}
        await CrudOperations.DeleteAsync(tbd, _dbContext);
    }}";
    }

    public string GetFindAllMethodCode(string modelName)
    {
        return $@"public async Task<List<{modelName}>> FindAllAsync{modelName}()
    {{
        return await CrudOperations.FindAllAsync<{modelName}>(_dbContext);
    }}";
    }

    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return
            $@"public async Task<{modelName}?> Find{modelName}ByIdAsync({GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        return await CrudOperations.FindByIdAsync<{modelName}>(_dbContext, {GetMethodInputForPrimaryKeys(primaryKeys, true)});
    }}";
    }

    public string GetUpdateMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return $@"public async Task Update{modelName}({modelName} updated{modelName})
    {{
        await CrudOperations.UpdateAsync(updated{modelName}, _dbContext);
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