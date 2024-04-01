using System.Data;
using System.Data.Common;
using Npgsql;

namespace GrpcGenerator.Utils;

public static class DatabaseSchemaUtils
{
    private static readonly Dictionary<string, Func<string, DbConnection>> ConnectionGetters = new()
    {
        { "postgres", connectionString => new NpgsqlConnection(connectionString) }
    };

    private static readonly Dictionary<string, Func<string, DbConnection, DbDataAdapter>> DataAdapterGetters = new()
    {
        { "postgres", (tableName, conn) => new NpgsqlDataAdapter("select * from " + tableName, (NpgsqlConnection)conn) }
    };

    public static List<string> FindTablesAndExecuteActionForEachTable(string uuid, string provider,
        string connectionString, Action<string, Dictionary<string, Type>> action)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var conn = ConnectionGetters[provider].Invoke(connectionString);
        var models = Directory.GetFiles($"{generatorVariables.ProjectDirectory}/Domain/Models");
        conn.Open();
        var allTablesSchemaTable = conn.GetSchema("Tables");
        var modelNames = new List<string>();
        foreach (DataRow tableInfo in allTablesSchemaTable.Rows)
        {
            var tableName = (string)tableInfo.ItemArray[2]!;
            using var adapter = DataAdapterGetters[provider].Invoke(tableName, conn);
            using var table = new DataTable(tableName);
            var primaryKeys = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .ToDictionary(c => StringUtils.GetDotnetNameFromSqlName(c.ColumnName), c => c.DataType)!;
            var modelName = StringUtils.GetDotnetNameFromSqlName(tableName);
            if (!models.Any(file => file.EndsWith($"{modelName}.cs"))) continue;
            action.Invoke(modelName, primaryKeys);
            modelNames.Add(modelName);
        }

        conn.Close();
        return modelNames;
    }

    public static string GetMethodInputForPrimaryKeys(IReadOnlyDictionary<string, Type> primaryKeys, bool call, string? prefix = null)
    {
        var result = "";
        var i = 0;
        foreach (var entry in primaryKeys)
        {
            if (i != 0) result += ", ";
            var name = prefix == null ? char.ToLower(entry.Key[0]) + entry.Key[1..] : prefix + entry.Key;
            result += $"{(call ? "" : entry.Value + " ")}{name}";
            i++;
        }

        return result;
    }

    public static List<string> GetPrimaryKeysForModel(string provider,
        string connectionString, string modelName)
    {
        var conn = ConnectionGetters[provider].Invoke(connectionString);
        conn.Open();
        var allTablesSchemaTable = conn.GetSchema("Tables");
        List<string> result = new();
        foreach (DataRow tableInfo in allTablesSchemaTable.Rows)
        {
            var tableName = (string)tableInfo.ItemArray[2]!;
            var model = StringUtils.GetDotnetNameFromSqlName(tableName);
            if (model != modelName)
            {
                continue;
            }
            using var adapter = DataAdapterGetters[provider].Invoke(tableName, conn);
            using var table = new DataTable(tableName);
            var primaryKeys = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .Select(c => StringUtils.GetDotnetNameFromSqlName(c.ColumnName))
                .ToList()!;
            result.AddRange(primaryKeys);
        }

        conn.Close();
        return result;
    }
    
    public static Dictionary<string, Type> GetPrimaryKeysAndTypesForModel(string provider,
        string connectionString, string modelName)
    {
        var conn = ConnectionGetters[provider].Invoke(connectionString);
        conn.Open();
        var allTablesSchemaTable = conn.GetSchema("Tables");
        Dictionary<string, Type> result = new();
        foreach (DataRow tableInfo in allTablesSchemaTable.Rows)
        {
            var tableName = (string)tableInfo.ItemArray[2]!;
            var model = StringUtils.GetDotnetNameFromSqlName(tableName);
            if (model != modelName)
            {
                continue;
            }
            using var adapter = DataAdapterGetters[provider].Invoke(tableName, conn);
            using var table = new DataTable(tableName);
            result = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .ToDictionary(c => StringUtils.GetDotnetNameFromSqlName(c.ColumnName), c => c.DataType)!;
        }

        conn.Close();
        return result;
    }
    
    public static Dictionary<string, Dictionary<string, Type>> GetForeignKeys(string uuid,string tableName)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        
        var modelFile = $"{generatorVariables.ProjectDirectory}/Domain/Models/{tableName}.cs";
        var variables = File.ReadLines(modelFile).Where(line =>
            line.Contains("public ") && !line.Contains("class ") && !line.Contains("(") &&
            line.Contains("virtual ") && !line.Contains("ICollection"));
        return variables.Select(variable => variable.Split(" ")[10]).ToDictionary(foreignTable => foreignTable, foreignTable => DatabaseSchemaUtils.GetPrimaryKeysAndTypesForModel(generatorVariables.DatabaseProvider, generatorVariables.DatabaseConnection.ToConnectionString(), foreignTable));
    }
}