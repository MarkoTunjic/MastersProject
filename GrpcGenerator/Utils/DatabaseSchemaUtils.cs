using System.Data;
using System.Data.Common;
using GrpcGenerator.Domain;
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

    private static readonly Dictionary<string, string> ForeignKeysQuery = new()
    {
        {
            "postgres", @"SELECT DISTINCT
    ccu.table_name AS foreign_table_name,
    kcu.column_name
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
JOIN information_schema.columns col
	ON kcu.table_name = col.table_name
	AND kcu.column_name = col.column_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_schema = @tableSchema
    AND tc.table_name = @tableName;"
        }
    };

    private static readonly Dictionary<string, Func<DbParameterVariables, DbParameter>> TableParamGetter = new()
    {
        {
            "postgres", paramVariables => new NpgsqlParameter
            {
                Value = paramVariables.Value,
                ParameterName = paramVariables.ParamName,
                DbType = paramVariables.Type,
                Size = paramVariables.Size
            }
        }
    };

    private static readonly Dictionary<string, Action<string, List<DbParameterVariables>, DbCommand>>
        ForeignKeysCommandPrepearer = new()
        {
            {
                "postgres", (provider, dbParamVariables, command) =>
                {
                    foreach (var dbParamVariable in dbParamVariables)
                        command.Parameters.Add(TableParamGetter[provider].Invoke(dbParamVariable));
                }
            }
        };

    public static List<string> FindTablesAndExecuteActionForEachTable(string uuid, string provider,
        string connectionString,
        Action<string, Dictionary<string, Type>, Dictionary<string, Dictionary<ForeignKey, Type>>> action)
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
            var tableSchema = (string)tableInfo.ItemArray[1]!;
            using var adapter = DataAdapterGetters[provider].Invoke(tableName, conn);
            using var table = new DataTable(tableName);
            var primaryKeys = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .ToDictionary(c => StringUtils.GetDotnetNameFromSqlName(c.ColumnName), c => c.DataType)!;
            var foreignKeys = GetForeignKeys(uuid, tableName, tableSchema);

            var modelName = StringUtils.GetDotnetNameFromSqlName(tableName);
            if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];

            if (!models.Any(file => file.EndsWith($"{modelName}.cs"))) continue;
            action.Invoke(modelName, primaryKeys, foreignKeys);
            modelNames.Add(modelName);
        }

        conn.Close();
        return modelNames;
    }

    public static string GetMethodInputForPrimaryKeys(IReadOnlyDictionary<string, Type> primaryKeys, bool call,
        string? prefix = null)
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

    public static string GetMethodInputForForeignKeys(Dictionary<ForeignKey, Type> foreignKeys, bool call,
        string? prefix = null)
    {
        var result = "";
        var i = 0;
        foreach (var entry in foreignKeys)
        {
            if (i != 0) result += ", ";
            var name = prefix == null
                ? char.ToLower(entry.Key.ForeignColumnName[0]) + entry.Key.ForeignColumnName[1..]
                : prefix + entry.Key.ForeignColumnName;
            result += $"{(call ? "" : entry.Value + " ")}{name}";
            i++;
        }

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
            if (char.ToLower(model[^1]) == 's') model = model[..^1];

            if (model != modelName) continue;
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

    public static Dictionary<string, Dictionary<ForeignKey, Type>> GetForeignKeys(string uuid, string tableName,
        string tableSchema)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        using var connection = ConnectionGetters[generatorVariables.DatabaseProvider]
            .Invoke(generatorVariables.DatabaseConnection.ToConnectionString());
        connection.Open();

        using var sqlCommand = connection.CreateCommand();
        sqlCommand.CommandText = ForeignKeysQuery[generatorVariables.DatabaseProvider];

        ForeignKeysCommandPrepearer[generatorVariables.DatabaseProvider].Invoke(
            generatorVariables.DatabaseProvider,
            new List<DbParameterVariables>
            {
                new(
                    "@tableName",
                    DbType.String,
                    StringUtils.GetSqlNameFromDotnetName(tableName),
                    100
                ),
                new(
                    "@tableSchema",
                    DbType.String,
                    tableSchema,
                    100
                )
            }
            , sqlCommand);

        sqlCommand.Prepare();
        var reader = sqlCommand.ExecuteReader();
        var result = new Dictionary<string, Dictionary<ForeignKey, Type>>();
        if (!reader.HasRows) return result;
        while (reader.Read())
        {
            var foreignTableName = StringUtils.GetDotnetNameFromSqlName(reader.GetString("foreign_table_name"));
            var columnName = StringUtils.GetDotnetNameFromSqlName(reader.GetString("column_name"));
            if (char.ToLower(foreignTableName[^1]) == 's') foreignTableName = foreignTableName[..^1];

            result[foreignTableName] = GetPrimaryKeysAndTypesForModel(generatorVariables.DatabaseProvider,
                    generatorVariables.DatabaseConnection.ToConnectionString(),
                    StringUtils.GetDotnetNameFromSqlName(foreignTableName))
                .ToDictionary(entry => new ForeignKey(columnName, entry.Key), entry => entry.Value);
        }

        return result;
    }
}