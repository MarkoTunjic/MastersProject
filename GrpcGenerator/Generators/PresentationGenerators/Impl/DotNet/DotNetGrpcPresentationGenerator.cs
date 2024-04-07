using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.DotNet;

public class DotNetGrpcPresentationGenerator : IPresentationGenerator
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
        using var stream = new StreamWriter(File.Create($"{generatorVariables.ProjectDirectory}/Protos/protofile.proto"));
        stream.Write($@"syntax = proto3

{GetServices(uuid)}

{GetMessages(uuid)}
");
    }

    private static string GetServices(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var result = "";
        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnection.ToConnectionString(),
            (className, _, _) =>
            {
                if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs"))
                {
                    return;
                }
                
                result += $@"service {className}Service {{
    rpc Get{className}ById ({className}IdRequest) returns {className}Reply;
    rpc FindAll{className} (google.protobuf.empty) returns {className}ListReply;
    rpc Delete{className}ById ({className}IdRequest) returns google.protobuf.empty;
    rpc Update{className} ({className}UpdateRequest) returns google.protobuf.empty;
    rpc Create{className} ({className}CreateRequest) returns {className}Reply;
}}

";
            });
        return result;
    }

    private static string GetMessages(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var result = "";
        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnection.ToConnectionString(),
            (className, primaryKeys, foreignKeys) =>
            {
                if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs"))
                {
                    return;
                }
                
                result += $@"{GetIdRequestMessage(primaryKeys, className)}

{GetReplyMessage(generatorVariables, className)}

{GetListReplyMessage(className)}

{GetUpdateRequestMessage(generatorVariables, primaryKeys, className)}

{GetCreateRequestMessage(uuid, foreignKeys, className)}

";
            });
        return result;
    }

    private static string GetIdRequestMessage(Dictionary<string,Type> primaryKeys, string className)
    {
        var result = $"message {className}IdRequest {{";
        var i = 1;
        foreach (var entry in primaryKeys)
        {
            result += $"\n\t{DotNetToGrpcType[entry.Value]} {char.ToLower(entry.Key[0]) + entry.Key[1..]} = {i};";
            i++;
        }
        return result+"\n}";
    }

    private static string GetReplyMessage(GeneratorVariables generatorVariables, string className)
    {
        var reply = $"message {className}Reply{{";
        var fields = File.ReadLines($"{generatorVariables.ProjectDirectory}/Domain/Dto/{className}Dto.cs")
            .Where(line=>!line.Contains("class") && line.Contains("public"))
            .Select(line=>line.Trim());
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

    private static string GetUpdateRequestMessage(GeneratorVariables generatorVariables, Dictionary<string,Type> primaryKeys, string className)
    {
        var result = $"message {className}UpdateRequest {{";
        var i = 1;
        foreach (var entry in primaryKeys)
        {
            result += $"\n\t{DotNetToGrpcType[entry.Value]} {char.ToLower(entry.Key[0]) + entry.Key[1..]} = {i};";
            i++;
        }
        
        var fields = File.ReadLines($"{generatorVariables.ProjectDirectory}/Domain/Request/{className}WriteDto.cs")
            .Where(line=>!line.Contains("class") && line.Contains("public"))
            .Select(line=>line.Trim());
        foreach (var field in fields)
        {
            var split = field.Split(" ");
            var type = split[1];
            var name = split[2];
            result += $"\n\t{DotNetStringToGrpcType[type]} {char.ToLower(name[0]) + name[1..]} = {i};";
            i++;
        }
        return result+"\n}";
    }

    private static string GetCreateRequestMessage(string uuid, Dictionary<string, Dictionary<string,Type>> foreignKeys, string className)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        
        var result = $"message {className}CreateRequest {{";
        var i = 1;
        var fields = File.ReadLines($"{generatorVariables.ProjectDirectory}/Domain/Request/{className}WriteDto.cs")
            .Where(line=>!line.Contains("class") && line.Contains("public"))
            .Select(line=>line.Trim());
        foreach (var field in fields)
        {
            var split = field.Split(" ");
            var type = split[1];
            var name = split[2];
            result += $"\n\t{DotNetStringToGrpcType[type]} {char.ToLower(name[0]) + name[1..]} = {i};";
            i++;
        }
        
        foreach (var entry in foreignKeys)
        {
            foreach (var fkey in entry.Value)
            {
                result += $"\n\t{DotNetToGrpcType[fkey.Value]} {char.ToLower(entry.Key[0]) + entry.Key[1..] + fkey.Key} = {i};";
                i++;
            }
        }
        
        return result+"\n}";
    }

    private static string GetListReplyMessage(string className)
    {
        return  $@"message {className}ListReply{{
    repeated {className}Reply {className}s = 1;
}}
";
    }
}