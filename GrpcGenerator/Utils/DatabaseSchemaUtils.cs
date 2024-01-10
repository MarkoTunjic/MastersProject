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
}