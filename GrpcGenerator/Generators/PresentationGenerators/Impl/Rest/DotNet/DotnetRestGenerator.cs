using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.PresentationGenerators.Impl.Rest.DotNet;

public class DotnetRestGenerator : IPresentationGenerator
{
    public void GeneratePresentation(string uuid)
    {
        GenerateControllers(uuid);
        GenerateServiceRegistration(uuid);
        GenerateWebAppRegistration(uuid);
    }

    private static void GenerateControllers(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Presentation/Controllers");
        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnection.ToConnectionString(),
            (tableName, primaryKeys, foreignKeys) =>
            {
                var modelName = StringUtils.GetDotnetNameFromSqlName(tableName);
                if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];
                if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{modelName}.cs")) return;
                DotNetUtils.CovertPrimaryKeysAndForeignKeysToDotnetNames(ref primaryKeys, ref foreignKeys);
                GenerateController(uuid, modelName, primaryKeys, foreignKeys);
            });
    }

    private static void GenerateController(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var route = "";
        foreach (var entry in foreignKeys)
        {
            route += $"/{char.ToLower(entry.Key[0]) + entry.Key[1..]}s";
            route = entry.Value.Select(fk => char.ToLower(fk.Key.ColumnName[0]) + fk.Key.ColumnName[1..])
                .Aggregate(route, (current, param) => current + $"/{{{param}}}");
        }

        var modelFieldName = char.ToLower(modelName[0]) + modelName[1..];

        using var stream =
            new StreamWriter(File.Create(
                $"{generatorVariables.ProjectDirectory}/Presentation/Controllers/{modelName}Controller.cs"));
        stream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace};
using Microsoft.AspNetCore.Mvc;

namespace {generatorVariables.ProjectName}.{NamespaceNames.ControllersNamespace};
[ApiController]
[Route(""/api/[controller]"")]
public class {modelName}Controller : ControllerBase
{{
    private readonly I{modelName}Service _{modelFieldName}Service;
    public {modelName}Controller(I{modelName}Service {modelFieldName}Service)
    {{
        _{modelFieldName}Service = {modelFieldName}Service;
    }}

    {GetFindAllMethodCode(modelName, foreignKeys)}

    {GetFindByIdMethodCode(modelName, primaryKeys)}

    {GetDeleteByIdMethodCode(modelName, primaryKeys)}

    {GetUpdateMethodCode(modelName, primaryKeys)}

    {GetCreateMethodCode(modelName, foreignKeys)}
{GetFindByForeignKeyMethodCodes(modelName, foreignKeys)}
}}
");
    }

    private static string GetFindAllMethodCode(string modelName,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var service = $"_{char.ToLower(modelName[0]) + modelName[1..]}Service";

        return $@"[HttpGet]
    public async Task<ActionResult<List<{modelName}Dto>>> FindAll{modelName}s()
    {{
        return Ok(await {service}.FindAll{modelName}sAsync());
    }}";
    }

    private static string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        var route = primaryKeys.Aggregate("",
            (current, entry) => current + $"/{{{char.ToLower(entry.Key[0]) + entry.Key[1..]}}}");
        route = route[1..];
        var service = $"_{char.ToLower(modelName[0]) + modelName[1..]}Service";

        return $@"[HttpGet]
    [Route(""{route}/{modelName}"")]
    public async Task<ActionResult<{modelName}Dto>> Find{modelName}ById({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        return Ok(await {service}.Find{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)}));
    }}";
    }

    private static string GetDeleteByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        var route = primaryKeys.Aggregate("",
            (current, entry) => current + $"/{{{char.ToLower(entry.Key[0]) + entry.Key[1..]}}}");
        route = route[1..];
        var service = $"_{char.ToLower(modelName[0]) + modelName[1..]}Service";

        return $@"[HttpDelete]
    [Route(""{route}/{modelName}"")]
    public async Task<ActionResult> Delete{modelName}ById({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        await {service}.Delete{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)});
        return NoContent();
    }}";
    }

    private static string GetUpdateMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        var route = primaryKeys.Aggregate("",
            (current, entry) => current + $"/{{{char.ToLower(entry.Key[0]) + entry.Key[1..]}}}");
        route = route[1..];
        var service = $"_{char.ToLower(modelName[0]) + modelName[1..]}Service";

        return $@"[HttpPut]
    [Route(""{route}/{modelName}"")]
    public async Task<ActionResult> Update{modelName}({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)}, [FromBody] {modelName}WriteDto updated{modelName})
    {{
        await {service}.Update{modelName}Async({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)}, updated{modelName});
        return NoContent();
    }}";
    }

    private static string GetCreateMethodCode(string modelName,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var route = "";
        foreach (var entry in foreignKeys)
        {
            route += $"/{char.ToLower(entry.Key[0]) + entry.Key[1..]}s";
            route = entry.Value.Aggregate(route,
                (current, fk) => current + $"/{{{char.ToLower(fk.Key.ColumnName[0]) + fk.Key.ColumnName[1..]}}}");
        }

        route = route.Length > 0 ? route[1..] : "";
        var service = $"_{char.ToLower(modelName[0]) + modelName[1..]}Service";

        var foreignKeyMethodArguments = foreignKeys.OrderBy(entry => entry.Key).Aggregate("",
            (current, keyValuePair) =>
                current +
                $", {DatabaseSchemaUtils.GetMethodInputForForeignKeys(keyValuePair.Value, false, char.ToLower(keyValuePair.Key[0]) + keyValuePair.Key[1..])}");

        var foreignKeyCallArguments = foreignKeys.OrderBy(entry => entry.Key).Aggregate("",
            (current, keyValuePair) =>
                current +
                $", {DatabaseSchemaUtils.GetMethodInputForForeignKeys(keyValuePair.Value, true, char.ToLower(keyValuePair.Key[0]) + keyValuePair.Key[1..])}");

        return $@"[HttpPost]
    [Route(""{route}/{modelName}"")]
    public async Task<ActionResult<{modelName}Dto>> Create{modelName}([FromBody] {modelName}WriteDto new{modelName}{foreignKeyMethodArguments})
    {{
        return Ok(await {service}.Create{modelName}Async(new{modelName}{foreignKeyCallArguments}));
    }}";
    }

    private static string GetFindByForeignKeyMethodCodes(string modelName,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var service = $"_{char.ToLower(modelName[0]) + modelName[1..]}Service";
        var result = "";
        foreach (var entry in foreignKeys)
        {
            var foreignTableVariableName = char.ToLower(entry.Key[0]) + entry.Key[1..];
            var route = $"/{foreignTableVariableName}s";
            route = entry.Value.Aggregate(route,
                (current, fk) => current + $"/{{{char.ToLower(fk.Key.ColumnName[0]) + fk.Key.ColumnName[1..]}}}");
            result += $@"
    [HttpGet]
    [Route(""{route}/{modelName}"")]
    public async Task<ActionResult<List<{modelName}Dto>>> Find{modelName}sBy{entry.Key}Id({DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, false, foreignTableVariableName)})
    {{
        return Ok(await {service}.Find{modelName}sBy{entry.Key}Id({DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, true, foreignTableVariableName)}));
    }}

";
        }

        return result.Length > 0 ? result[..^2] : "";
    }

    private static void GenerateServiceRegistration(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        using var stream =
            new StreamWriter(
                File.Create(
                    $"{generatorVariables.ProjectDirectory}/Presentation/PresentationServiceRegistration.cs"));
        stream.Write($@"namespace {generatorVariables.ProjectName}.Presentation;
public static class PresentationServiceRegistration
{{
    public static void AddPresentation(this IServiceCollection services)
    {{
        services.AddControllers();
        services.AddSwaggerGen();
    }}
}}");
    }
    
    
    private static void GenerateWebAppRegistration(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        using var stream =
            new StreamWriter(
                File.Create(
                    $"{generatorVariables.ProjectDirectory}/Presentation/PresentationWebAppRegistration.cs"));
        stream.Write($@"namespace {generatorVariables.ProjectName}.Presentation;
public static class PresentationWebAppRegistration
{{
    public static void AddPresentation(this WebApplication app)
    {{
        app.MapControllers();
        app.UseSwagger();
        app.UseSwaggerUI();
    }}
}}");
    }
}