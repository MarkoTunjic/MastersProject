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
            GenerateRepository(modelName, primaryKeys, targetDirectory);
        }

        conn.Close();
    }

    public void GenerateRepository(string modelName, string[] primaryKeys, string targetDirectory)
    {
        throw new NotImplementedException();
    }

    public string GetCreateMethodCode(string modelName)
    {
        throw new NotImplementedException();
    }

    public string GetDeleteMethodCode(string modelName, string[] primaryKeys)
    {
        throw new NotImplementedException();
    }

    public string GetReadAllMethodCode(string modelName)
    {
        throw new NotImplementedException();
    }

    public string GetFindByIdMethodCode(string modelName, string[] primaryKeys)
    {
        throw new NotImplementedException();
    }


    private static string GetDotnetNameFromSqlName(string sqlName)
    {
        var result = "";
        foreach (var part in sqlName.Split("_"))
        {
            var firstLetter = char.ToUpper(part[1]);
            result += firstLetter + part[1..];
        }

        return result;
    }
}