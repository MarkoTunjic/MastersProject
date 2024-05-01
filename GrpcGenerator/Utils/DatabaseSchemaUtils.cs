using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using GrpcGenerator.Domain;
using Npgsql;

namespace GrpcGenerator.Utils;

public static class DatabaseSchemaUtils
{
    private static readonly Dictionary<string, Func<string, DbConnection>> ConnectionGetters = new()
    {
        { "postgres", connectionString => new NpgsqlConnection(connectionString) },
        {"sqlserver", connectionString => new SqlConnection(connectionString)}
    };

    private static readonly Dictionary<string, Func<string, DbConnection, DbDataAdapter>> DataAdapterGetters = new()
    {
        { "postgres", (tableName, conn) => new NpgsqlDataAdapter("select * from " + tableName, (NpgsqlConnection)conn) },
        {"sqlserver", (tableName, conn) => new SqlDataAdapter("select * from " + tableName, (SqlConnection) conn)}
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
        },
        {"sqlserver", @"SELECT col1.name AS column_name,
    tab2.name AS foreign_table_name
FROM sys.foreign_key_columns fkc
INNER JOIN sys.objects obj
    ON obj.object_id = fkc.constraint_object_id
INNER JOIN sys.tables tab1
    ON tab1.object_id = fkc.parent_object_id AND tab1.name = @tableName
INNER JOIN sys.schemas sch
    ON tab1.schema_id = sch.schema_id
    AND sch.name = @tableSchema
INNER JOIN sys.columns col1
    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
INNER JOIN sys.tables tab2
    ON tab2.object_id = fkc.referenced_object_id"}
    };
    
    private static readonly Dictionary<string, string> ReferencedTablesQuery = new()
    {
        {
            "postgres", @"SELECT DISTINCT
    tc.table_name AS table_name,
    kcu.column_name AS column_name
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
    AND ccu.table_name = @tableName;"
        },
        {"sqlserver", @"SELECT tab1.name AS table_name,
       col1.name AS column_name
FROM sys.foreign_key_columns fkc
INNER JOIN sys.objects obj
    ON obj.object_id = fkc.constraint_object_id
INNER JOIN sys.tables tab1
    ON tab1.object_id = fkc.parent_object_id
INNER JOIN sys.tables tab2
    ON tab2.object_id = fkc.referenced_object_id AND tab2.name = @tableName
INNER JOIN sys.schemas sch
    ON tab2.schema_id = sch.schema_id
INNER JOIN sys.columns col1
    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id"}
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
        },
        {
            "sqlserver", paramVariables => new SqlParameter
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
            },
            {
                "sqlserver", (provider, dbParamVariables, command) =>
                {
                    foreach (var dbParamVariable in dbParamVariables)
                        command.Parameters.Add(TableParamGetter[provider].Invoke(dbParamVariable));
                }
            }
        };

    public static List<string> FindTablesAndExecuteActionForEachTable(string uuid, string provider,
        string connectionString,
        Action<string, Dictionary<string, Type>, Dictionary<string, Dictionary<ForeignKey, Type>>>? action, Func<string, bool>? shouldExecute=null)
    {
        var conn = ConnectionGetters[provider].Invoke(connectionString);
        conn.Open();
        var allTablesSchemaTable = conn.GetSchema("Tables");
        var tableNames = new List<string>();
        foreach (DataRow tableInfo in allTablesSchemaTable.Rows)
        {
            var tableName = (string)tableInfo.ItemArray[2]!;
            tableNames.Add(tableName);
            if (shouldExecute != null && !shouldExecute.Invoke(tableName))
            {
                continue;
            }
            var tableSchema = (string)tableInfo.ItemArray[1]!;
            using var adapter = DataAdapterGetters[provider].Invoke(tableName, conn);
            using var table = new DataTable(tableName);
            var primaryKeys = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .ToDictionary(c => c.ColumnName, c => c.DataType)!;
            var foreignKeys = GetForeignKeys(uuid, tableName, tableSchema);
            action?.Invoke(tableName, primaryKeys, foreignKeys);
        }

        conn.Close();
        return tableNames;
    }

    public static string GetMethodInputForPrimaryKeys(IReadOnlyDictionary<string, Type> primaryKeys, bool call,
        string? prefix = null)
    {
        var result = "";
        var i = 0;
        foreach (var entry in primaryKeys.OrderBy(entry => entry.Key))
        {
            if (i != 0) result += ", ";
            var name = prefix == null
                ? char.ToLower(entry.Key[0]) + entry.Key[1..]
                : prefix + char.ToUpper(entry.Key[0]) + entry.Key[1..];
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
                : prefix + char.ToUpper(entry.Key.ForeignColumnName[0]) + entry.Key.ForeignColumnName[1..];
            result += $"{(call ? "" : entry.Value + " ")}{name}";
            i++;
        }

        return result;
    }

    public static Dictionary<string, Type> GetPrimaryKeysAndTypesForModel(string provider,
        string connectionString, string tableName)
    {
        var conn = ConnectionGetters[provider].Invoke(connectionString);
        conn.Open();
        var allTablesSchemaTable = conn.GetSchema("Tables");
        Dictionary<string, Type> result = new();
        foreach (DataRow tableInfo in allTablesSchemaTable.Rows)
        {
            var currentTable = (string)tableInfo.ItemArray[2]!;

            if (currentTable != tableName) continue;
            using var adapter = DataAdapterGetters[provider].Invoke(currentTable, conn);
            using var table = new DataTable(currentTable);
            result = adapter
                .FillSchema(table, SchemaType.Mapped)
                ?.PrimaryKey
                .ToDictionary(c => c.ColumnName, c => c.DataType)!;
        }

        conn.Close();
        return result;
    }

    private static Dictionary<string, Dictionary<ForeignKey, Type>> GetForeignKeys(string uuid, string tableName,
        string tableSchema)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        using var connection = ConnectionGetters[generatorVariables.DatabaseProvider]
            .Invoke(generatorVariables.DatabaseConnectionData.ToConnectionString());
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
            var foreignTableName = reader.GetString("foreign_table_name");
            var columnName = reader.GetString("column_name");

            result[foreignTableName] = GetPrimaryKeysAndTypesForModel(generatorVariables.DatabaseProvider,
                    generatorVariables.DatabaseConnectionData.ToConnectionString(),
                    foreignTableName)
                .ToDictionary(entry => new ForeignKey(columnName, entry.Key), entry => entry.Value);
        }

        return result;
    }
    
    public static List<string> GetCascadeReferencedTables(string uuid, string tableName)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        using var connection = ConnectionGetters[generatorVariables.DatabaseProvider]
            .Invoke(generatorVariables.DatabaseConnectionData.ToConnectionString());
        connection.Open();

        using var sqlCommand = connection.CreateCommand();
        sqlCommand.CommandText = ReferencedTablesQuery[generatorVariables.DatabaseProvider];

        ForeignKeysCommandPrepearer[generatorVariables.DatabaseProvider].Invoke(
            generatorVariables.DatabaseProvider,
            new List<DbParameterVariables>
            {
                new(
                    "@tableName",
                    DbType.String,
                    StringUtils.GetSqlNameFromDotnetName(tableName),
                    100
                )
            }
            , sqlCommand);

        sqlCommand.Prepare();
        var reader = sqlCommand.ExecuteReader();
        var result = new List<string>();
        if (!reader.HasRows) return result;
        while (reader.Read())
        {
            var table = reader.GetString("table_name");
            result.Add(table);
        }

        return result;
    }
}