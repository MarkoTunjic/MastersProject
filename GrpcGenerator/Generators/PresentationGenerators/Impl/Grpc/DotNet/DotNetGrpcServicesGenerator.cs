using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.Grpc.DotNet;

public class DotNetGrpcServicesGenerator : IPresentationGenerator
{
    public void GeneratePresentation(string uuid)
    {
        GenerateServices(uuid);
    }

    private static void GenerateServices(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Presentation/Grpc");
        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnectionData.ToConnectionString(),
            (tableName, primaryKeys, foreignKeys) =>
            {
                var className = StringUtils.GetDotnetNameFromSqlName(tableName);
                if (char.ToLower(className[^1]) == 's') className = className[..^1];
                DotNetUtils.ConvertPrimaryKeysAndForeignKeysToDotnetNames(ref primaryKeys, ref foreignKeys);

                if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs")) return;
                var variableName = char.ToLower(className[0]) + className[1..];
                
                var cascadeService = generatorVariables.Cascade ? "private readonly CascadeDeleteService _cascadeDeleteService;" : "";
                var cascadeServiceArgument = generatorVariables.Cascade ? ", CascadeDeleteService cascadeDeleteService" : "";
                var cascadeServiceAssignment = generatorVariables.Cascade ? "_cascadeDeleteService = cascadeDeleteService;" : "";

                using var stream =
                    new StreamWriter(File.Create(
                        $"{generatorVariables.ProjectDirectory}/Presentation/Grpc/Grpc{className}ServiceImpl.cs"));
                stream.Write(@$"using {generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace};
using AutoMapper;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

namespace {generatorVariables.ProjectName}.Presentation.Grpc;
public class Grpc{className}ServiceImpl : Grpc{className}Service.Grpc{className}ServiceBase
{{
    private readonly I{className}Service _{variableName}Service;
    private readonly IMapper _mapper;
    {cascadeService}

    public Grpc{className}ServiceImpl(I{className}Service {variableName}Service, IMapper mapper{cascadeServiceArgument})
    {{
        _{variableName}Service = {variableName}Service;
        _mapper = mapper;
        {cascadeServiceAssignment}
    }}
    
    {GetFindByIdMethodCode(className, primaryKeys)}

    {GetFindAllMethodCode(className)}

    {GetDeleteByIdMethodCode(generatorVariables.Cascade, className, primaryKeys)}
    
    {GetUpdateMethodCode(className, primaryKeys)}

    {GetCreateMethodCode(className, foreignKeys)}
    
{GetFindByForeignKeyMethodCodes(className, foreignKeys)}
}}");
            },tableName=>generatorVariables.IncludedTables==null || generatorVariables.IncludedTables.Count == 0 || generatorVariables.IncludedTables.Contains(tableName));
    }

    private static string GetFindByIdMethodCode(string className, Dictionary<string, Type> primaryKeys)
    {
        var variableName = char.ToLower(className[0]) + className[1..];

        return
            $@"public override async Task<{className}Reply> Get{className}ById({className}IdRequest {variableName}Id, ServerCallContext context)
    {{
        var result = await _{variableName}Service.Find{className}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true, $"{variableName}Id.")});
        return _mapper.Map<{className}Dto, {className}Reply>(result);
    }}";
    }

    private static string GetFindAllMethodCode(string className)
    {
        var variableName = char.ToLower(className[0]) + className[1..];

        return
            $@"public override async Task<{className}ListReply> FindAll{className}s(Empty empty, ServerCallContext context)
    {{
        var result = await _{variableName}Service.FindAll{className}sAsync();
        var mappedResult = _mapper.Map<List<{className}Dto>, List<{className}Reply>>(result);
                
        return new {className}ListReply()
        {{
            {className}s = {{ mappedResult }}
        }};
    }}";
    }

    private static string GetDeleteByIdMethodCode(bool cascade, string className, Dictionary<string, Type> primaryKeys)
    {
        var variableName = char.ToLower(className[0]) + className[1..];
        var service = cascade ? "_cascadeDeleteService" : $"_{char.ToLower(className[0]) + className[1..]}Service";

        return
            $@"public override async Task<Empty> Delete{className}ById({className}IdRequest {variableName}Id, ServerCallContext context)
    {{
        await {service}.Delete{className}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true, $"{variableName}Id.")});
    
        return new Empty();
    }}";
    }

    private static string GetUpdateMethodCode(string className, Dictionary<string, Type> primaryKeys)
    {
        var variableName = char.ToLower(className[0]) + className[1..];

        return
            $@"public override async Task<Empty> Update{className}({className}UpdateRequest {variableName}, ServerCallContext context)
    {{
        var mappedInput = _mapper.Map<{className}UpdateRequest, {className}WriteDto>({variableName});
        await _{variableName}Service.Update{className}Async({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true, $"{variableName}.")}, mappedInput);
    
        return new Empty();
    }}";
    }

    private static string GetCreateMethodCode(string className,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var variableName = char.ToLower(className[0]) + className[1..];
        var foreignKeyInput = foreignKeys.OrderBy(entry => entry.Key).Aggregate("",
            (current, entry) =>
                current +
                $", {DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, true, $"{variableName}.{entry.Key}")}");
        return
            $@"public override async Task<{className}Reply> Create{className}({className}CreateRequest {variableName}, ServerCallContext context)
    {{
        var mappedInput = _mapper.Map<{className}CreateRequest, {className}WriteDto>({variableName});
        var result = await _{variableName}Service.Create{className}Async(mappedInput{foreignKeyInput});

        return _mapper.Map<{className}Dto, {className}Reply>(result);
    }}";
    }

    private static string GetFindByForeignKeyMethodCodes(string className,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var variableName = char.ToLower(className[0]) + className[1..];
        var result = "";
        foreach (var entry in foreignKeys)
        {
            var foreignVariableName = char.ToLower(entry.Key[0]) + entry.Key[1..];
            result +=
                $@"   public override async Task<{className}ListReply> Find{className}sBy{entry.Key}Id({entry.Key}IdRequest {foreignVariableName}Id, ServerCallContext context)
    {{
        var result = await _{variableName}Service.Find{className}sBy{entry.Key}Id({DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, true, $"{foreignVariableName}Id.")});
        var mappedResult = _mapper.Map<List<{className}Dto>, List<{className}Reply>>(result);
        
        return new {className}ListReply()
        {{
            {className}s = {{ mappedResult }}
        }};
    }}

";
        }

        if (result.Length > 2) result = result[..^2];
        return result;
    }
}