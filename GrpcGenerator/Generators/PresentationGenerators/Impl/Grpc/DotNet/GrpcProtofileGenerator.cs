using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.Grpc.DotNet;

public class GrpcProtofileGenerator : IPresentationGenerator
{
    private static readonly Dictionary<Type, string> DotNetToGrpcType = new()
    {
        { typeof(int), "int32" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(long), "int64" },
        { typeof(uint), "uint32" },
        { typeof(ulong), "uint64" },
        { typeof(bool), "bool" },
        { typeof(string), "string" }
    };

    private static readonly Dictionary<string, string> DotNetStringToGrpcType = new()
    {
        { "int", "int32" },
        { "double", "double" },
        { "float", "float" },
        { "long", "int64" },
        { "uint", "uint32" },
        { "ulong", "uint64" },
        { "bool", "bool" },
        { "string", "string" }
    };


    public void GeneratePresentation(string uuid)
    {
        GenerateProtofile(uuid);
    }

    private static void GenerateProtofile(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Protos");
        using var stream =
            new StreamWriter(File.Create($"{generatorVariables.ProjectDirectory}/Protos/protofile.proto"));
        stream.Write($@"syntax = ""proto3"";
import ""google/protobuf/empty.proto"";

{GetServices(uuid)}

{GetMessages(uuid)}
");
    }

    private static string GetServices(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var result = "";
        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnectionData.ToConnectionString(),
            (className, primaryKeys, foreignKeys) =>
            {
                className = StringUtils.GetDotnetNameFromSqlName(className);
                if (char.ToLower(className[^1]) == 's') className = className[..^1];
                DotNetUtils.ConvertPrimaryKeysAndForeignKeysToDotnetNames(ref primaryKeys, ref foreignKeys);

                if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs")) return;

                var findByForeignKey = foreignKeys.Aggregate("",
                    (current, entry) =>
                        current +
                        $"    rpc Find{className}sBy{entry.Key}Id ({entry.Key}IdRequest) returns ({className}ListReply) {{}}\n");
                result += $@"service Grpc{className}Service {{
    rpc Get{className}ById ({className}IdRequest) returns ({className}Reply) {{}}
    rpc FindAll{className}s (google.protobuf.Empty) returns ({className}ListReply) {{}}
    rpc Delete{className}ById ({className}IdRequest) returns (google.protobuf.Empty) {{}}
    rpc Update{className} ({className}UpdateRequest) returns (google.protobuf.Empty) {{}}
    rpc Create{className} ({className}CreateRequest) returns ({className}Reply) {{}}
{findByForeignKey.TrimEnd()}
}}

";
            },tableName=>generatorVariables.IncludedTables==null || generatorVariables.IncludedTables.Count == 0 || generatorVariables.IncludedTables.Contains(tableName));
        return result;
    }

    private static string GetMessages(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var result = "";
        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnectionData.ToConnectionString(),
            (className, primaryKeys, foreignKeys) =>
            {
                className = StringUtils.GetDotnetNameFromSqlName(className);
                if (char.ToLower(className[^1]) == 's') className = className[..^1];
                DotNetUtils.ConvertPrimaryKeysAndForeignKeysToDotnetNames(ref primaryKeys, ref foreignKeys);

                if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs")) return;

                result += $@"{GetIdRequestMessage(primaryKeys, className)}

{GetReplyMessage(generatorVariables, className)}

{GetListReplyMessage(className)}

{GetUpdateRequestMessage(generatorVariables, primaryKeys, className)}

{GetCreateRequestMessage(uuid, foreignKeys, className)}

";
            },tableName=>generatorVariables.IncludedTables==null || generatorVariables.IncludedTables.Count == 0 || generatorVariables.IncludedTables.Contains(tableName));
        return result;
    }

    private static string GetIdRequestMessage(Dictionary<string, Type> primaryKeys, string className)
    {
        var result = $"message {className}IdRequest {{";
        var i = 1;
        foreach (var entry in primaryKeys)
        {
            result += $"\n\t{DotNetToGrpcType[entry.Value]} {char.ToLower(entry.Key[0]) + entry.Key[1..]} = {i};";
            i++;
        }

        return result + "\n}";
    }

    private static string GetReplyMessage(GeneratorVariables generatorVariables, string className)
    {
        var reply = $"message {className}Reply{{";
        var fields = File.ReadLines($"{generatorVariables.ProjectDirectory}/Domain/Dto/{className}Dto.cs")
            .Where(line => !line.Contains("class") && line.Contains("public"))
            .Select(line => line.Trim());
        var i = 1;
        foreach (var field in fields)
        {
            var split = field.Split(" ");
            var type = split[1];
            var name = split[2];
            reply += $"\n\t{DotNetStringToGrpcType[type]} {char.ToLower(name[0]) + name[1..]} = {i};";
            i++;
        }

        reply += "\n}";
        return reply;
    }

    private static string GetUpdateRequestMessage(GeneratorVariables generatorVariables,
        Dictionary<string, Type> primaryKeys, string className)
    {
        var result = $"message {className}UpdateRequest {{";
        var i = 1;
        foreach (var entry in primaryKeys)
        {
            result += $"\n\t{DotNetToGrpcType[entry.Value]} {char.ToLower(entry.Key[0]) + entry.Key[1..]} = {i};";
            i++;
        }

        var fields = File.ReadLines($"{generatorVariables.ProjectDirectory}/Domain/Request/{className}WriteDto.cs")
            .Where(line => !line.Contains("class") && line.Contains("public"))
            .Select(line => line.Trim());
        foreach (var field in fields)
        {
            var split = field.Split(" ");
            var type = split[1];
            var name = split[2];
            result += $"\n\t{DotNetStringToGrpcType[type]} {char.ToLower(name[0]) + name[1..]} = {i};";
            i++;
        }

        return result + "\n}";
    }

    private static string GetCreateRequestMessage(string uuid,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys, string className)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var result = $"message {className}CreateRequest {{";
        var i = 1;
        var fields = File.ReadLines($"{generatorVariables.ProjectDirectory}/Domain/Request/{className}WriteDto.cs")
            .Where(line => !line.Contains("class") && line.Contains("public"))
            .Select(line => line.Trim());
        foreach (var field in fields)
        {
            var split = field.Split(" ");
            var type = split[1];
            var name = split[2];
            result += $"\n\t{DotNetStringToGrpcType[type]} {char.ToLower(name[0]) + name[1..]} = {i};";
            i++;
        }

        foreach (var fkey in foreignKeys.SelectMany(entry => entry.Value))
        {
            result +=
                $"\n\t{DotNetToGrpcType[fkey.Value]} {char.ToLower(fkey.Key.ColumnName[0]) + fkey.Key.ColumnName[1..]} = {i};";
            i++;
        }

        return result + "\n}";
    }

    private static string GetListReplyMessage(string className)
    {
        return $@"message {className}ListReply{{
    repeated {className}Reply {className}s = 1;
}}
";
    }
}