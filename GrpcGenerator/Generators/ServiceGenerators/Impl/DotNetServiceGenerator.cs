using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.ServiceGenerators.Impl;

public class DotNetServiceGenerator : IServiceGenerator
{
    public void GenerateServices(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Application/Services/Impl");

        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Application/Services";

        var modelNames = DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, "postgres",
            generatorVariables.DatabaseConnection.ToConnectionString(),
            (modelName, primaryKeys, foreignKeys) =>
                GenerateService(uuid, modelName, primaryKeys, foreignKeys, targetDirectory));

        modelNames = modelNames.Select(modelName =>
        {
            modelName = StringUtils.GetDotnetNameFromSqlName(modelName);
            if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];
            return modelName;
        }).ToList();
        GenerateApplicationServiceRegistration(uuid, modelNames);
        GenerateNotFoundException(uuid);
    }

    private void GenerateNotFoundException(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Domain/Exceptions");

        using var stream = new StreamWriter(File.Create($"{generatorVariables.ProjectDirectory}/Domain/Exceptions/NotFoundException.cs"));
        stream.Write($@"namespace {NamespaceNames.ExceptionsNamespace};

public class NotFoundException : Exception
{{
    public NotFoundException()
    {{
    }}

    public NotFoundException(string message)
    : base(message)
    {{
    }}
    
    public NotFoundException(string message, Exception inner)
        : base(message, inner)
    {{
    }}
}}");
    }

    public void GenerateService(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys,
        string targetDirectory)
    {
        modelName = StringUtils.GetDotnetNameFromSqlName(modelName);
        if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];
        DotNetUtils.CovertPrimaryKeysAndForeignKeysToDotnetNames(ref primaryKeys, ref foreignKeys);

        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{modelName}.cs")) return;

        var createMethod = GetCreateMethodCode(modelName, foreignKeys);
        var deleteMethod = GetDeleteMethodCode(modelName, primaryKeys);
        var readAllMethod = GetFindAllMethodCode(modelName);
        var findById = GetFindByIdMethodCode(modelName, primaryKeys);
        var findByForeignKey = GetFindByForeignKeyMethods(modelName, foreignKeys);
        var updateMethod = GetUpdateMethodCode(modelName, primaryKeys);

        var findByForeignKeysSplit = findByForeignKey.Split("\n\n");
        var findByIdMethodDeclarations = findByForeignKeysSplit.Aggregate("",
            (current, method) => method.Length > 0
                ? current + $"{method[..method.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};\n"
                : "");
        findByIdMethodDeclarations = findByIdMethodDeclarations.Trim();
        using var interfaceStream = new StreamWriter(File.Create($"{targetDirectory}/I{modelName}Service.cs"));
        interfaceStream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace};
public interface I{modelName}Service
{{
    {createMethod[..createMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {deleteMethod[..deleteMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {readAllMethod[..readAllMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {findById[..findById.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {updateMethod[..updateMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {findByIdMethodDeclarations}
}}
");
        using var classStream = new StreamWriter(File.Create($"{targetDirectory}/Impl/{modelName}Service.cs"));
        classStream.Write($@"using {NamespaceNames.ModelsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};
using AutoMapper;

namespace {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace}.Impl;
public class {modelName}Service : I{modelName}Service
{{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public {modelName}Service(IUnitOfWork unitOfWork, IMapper mapper)
    {{
        this._unitOfWork = unitOfWork;
        this._mapper = mapper;
    }}
    
    {createMethod}

    {deleteMethod}

    {updateMethod}

    {readAllMethod}

    {findById}

{findByForeignKey}
}}
");
    }

    public string GetCreateMethodCode(string modelName, Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var foreignKeyMethodArguments = foreignKeys.Aggregate("",
            (current, keyValuePair) =>
                current +
                $", {DatabaseSchemaUtils.GetMethodInputForForeignKeys(keyValuePair.Value, false, char.ToLower(keyValuePair.Key[0]) + keyValuePair.Key[1..])}");
        var getAndSetForeignKeys = foreignKeys.Aggregate("",
            (current, keyValuePair) => current +
                                       $"\t\tmodel.{keyValuePair.Key} = await _unitOfWork.{keyValuePair.Key}Repository.Find{keyValuePair.Key}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForForeignKeys(keyValuePair.Value, true, char.ToLower(keyValuePair.Key[0]) + keyValuePair.Key[1..])});\n");
        return
            $@"public async Task<{modelName}Dto> Create{modelName}Async({modelName}WriteDto new{modelName}{foreignKeyMethodArguments})
    {{
        var model = _mapper.Map<{modelName}WriteDto, {modelName}>(new{modelName});

{getAndSetForeignKeys}
        return _mapper.Map<{modelName}, {modelName}Dto>(await _unitOfWork.{modelName}Repository.Create{modelName}Async(model));
    }}";
    }

    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return
            $@"public async Task Delete{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        await _unitOfWork.{modelName}Repository.Delete{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)});
    }}";
    }

    public string GetFindAllMethodCode(string modelName)
    {
        return $@"public async Task<List<{modelName}Dto>> FindAll{modelName}Async()
    {{
        return _mapper.Map<List<{modelName}>, List<{modelName}Dto>>(await _unitOfWork.{modelName}Repository.FindAll{modelName}Async());
    }}";
    }

    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return
            $@"public async Task<{modelName}Dto> Find{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        var result = await _unitOfWork.{modelName}Repository.Find{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)});
        return _mapper.Map<{modelName}, {modelName}Dto>(result);
    }}";
    }

    public string GetFindByForeignKeyMethods(string modelName,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys)
    {
        var result = "";
        foreach (var entry in foreignKeys)
        {
            var foreignEntity = char.ToLower(entry.Key[0]) + entry.Key[1..];
            result += "\t" +
                      $@"public async Task<List<{modelName}Dto>> Find{modelName}sBy{entry.Key}Id({DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, false, foreignEntity)})
    {{
        var {foreignEntity} = await _unitOfWork.{entry.Key}Repository.Find{entry.Key}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, true, foreignEntity)});
        return _mapper.Map<List<{modelName}>, List<{modelName}Dto>>({foreignEntity}.{modelName}s.ToList());
    }}

";
        }

        return result.Length == 0 ? result : result[..^2];
    }

    public string GetUpdateMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return
            $@"public async Task Update{modelName}Async({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)}, {modelName}WriteDto updated{modelName})
    {{
        var model = await _unitOfWork.{modelName}Repository.Find{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)});
        model = _mapper.Map<{modelName}WriteDto, {modelName}>(updated{modelName}, model);
        await _unitOfWork.{modelName}Repository.Update{modelName}Async(model);
    }}";
    }

    private static void GenerateApplicationServiceRegistration(string uuid, List<string> modelNames)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        using var stream =
            new StreamWriter(
                File.Create(
                    $"{generatorVariables.ProjectDirectory}/Application/ApplicationServiceRegistration.cs"));
        stream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace}.Impl;

namespace {generatorVariables.ProjectName}.Application;
public static class ApplicationServiceRegistration
{{
    public static void AddApplication(this IServiceCollection services)
    {{");
        foreach (var modelName in modelNames.Where(modelName =>
                     File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{modelName}.cs")))
            stream.WriteLine($"\t\tservices.AddTransient<I{modelName}Service, {modelName}Service>();");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}