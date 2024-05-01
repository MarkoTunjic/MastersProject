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

        var modelNames = DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnectionData.ToConnectionString(),
            (modelName, primaryKeys, foreignKeys) =>
                GenerateService(uuid, modelName, primaryKeys, foreignKeys, targetDirectory));
        GenerateCascadeDeleteService(uuid,modelNames);
        modelNames = modelNames.Select(modelName =>
        {
            modelName = StringUtils.GetDotnetNameFromSqlName(modelName);
            if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];
            return modelName;
        }).ToList();
        GenerateApplicationServiceRegistration(uuid, modelNames);
        GenerateNotFoundException(uuid);
    }

    public void GenerateService(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        Dictionary<string, Dictionary<ForeignKey, Type>> foreignKeys,
        string targetDirectory)
    {
        modelName = StringUtils.GetDotnetNameFromSqlName(modelName);
        if (char.ToLower(modelName[^1]) == 's') modelName = modelName[..^1];
        DotNetUtils.ConvertPrimaryKeysAndForeignKeysToDotnetNames(ref primaryKeys, ref foreignKeys);
        
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{modelName}.cs")) return;

        var createMethod = GetCreateMethodCode(modelName, foreignKeys);
        var deleteMethod = GetDeleteMethodCode(modelName, primaryKeys);
        var readAllMethod = GetFindAllMethodCode(modelName);
        var findById = GetFindByIdMethodCode(modelName, primaryKeys);
        var findByForeignKey = GetFindByForeignKeyMethods(modelName, foreignKeys);
        var updateMethod = GetUpdateMethodCode(modelName, primaryKeys);

        var findByForeignKeysSplit = findByForeignKey.Split("\n\n");
        var findByIdMethodDeclarations = findByForeignKeysSplit.Where(method=>method.Length > 0).Aggregate("",
            (current, method) => current + $"{method[..method.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};\n");
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
        var foreignKeyMethodArguments = foreignKeys.OrderBy(entry => entry.Key).Aggregate("",
            (current, keyValuePair) =>
                current +
                $", {DatabaseSchemaUtils.GetMethodInputForForeignKeys(keyValuePair.Value, false, char.ToLower(keyValuePair.Key[0]) + keyValuePair.Key[1..])}");
        var getAndSetForeignKeys = "";
        foreach (var key in foreignKeys)
        {
            getAndSetForeignKeys +=
                $"\t\tvar found{key.Key} = await _unitOfWork.{key.Key}Repository.Find{key.Key}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForForeignKeys(key.Value, true, char.ToLower(key.Key[0]) + key.Key[1..])});\n";
            getAndSetForeignKeys = key.Value.Aggregate(getAndSetForeignKeys, (current, fk) => current + $"\t\tmodel.{fk.Key.ColumnName} = found{key.Key}.{fk.Key.ForeignColumnName};\n");
        }
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
        return $@"public async Task<List<{modelName}Dto>> FindAll{modelName}sAsync()
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
        var result = await _unitOfWork.{modelName}Repository.Find{modelName}sBy{entry.Key}IdAsync({DatabaseSchemaUtils.GetMethodInputForForeignKeys(entry.Value, true, foreignEntity)});
        return _mapper.Map<List<{modelName}>, List<{modelName}Dto>>(result);
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

    private static void GenerateCascadeDeleteService(string uuid,List<string> tables)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var servicesDeclaration = "";
        var servicesAssignment = "";
        var servicesArguments = "";
        var deleteMethods = "";
        foreach (var table in tables)
        {
            var name = StringUtils.GetDotnetNameFromSqlName(table);
            if (char.ToLower(name[^1]) == 's') name = name[..^1];
            if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{name}.cs"))
            {
                continue;
            }
            var variableName = char.ToLower(name[0]) + name[1..];
            servicesDeclaration += $"\tprivate readonly I{name}Service _{variableName}Service;\n";
            servicesAssignment += $"\t\t_{variableName}Service = {variableName}Service;\n\n";
            servicesArguments += $", I{name}Service {variableName}Service";
            var primaryKeys = DatabaseSchemaUtils.GetPrimaryKeysAndTypesForModel(generatorVariables.DatabaseProvider,
                generatorVariables.DatabaseConnectionData.ToConnectionString(), table);
            DotNetUtils.ConvertPrimaryKeysToDotnetNames(ref primaryKeys);
            var references = DatabaseSchemaUtils.GetCascadeReferencedTables(uuid, table);
            DotNetUtils.ConvertStringListToDotNetNames(ref references);
            references = references.Where(el =>
                File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{el}.cs")).ToList();
            var cascadeDelete = "";
            foreach (var reference in references)
            {
                var referenceVariableName = char.ToLower(reference[0]) + reference[1..];
                cascadeDelete += $@"        var {referenceVariableName}References = await _unitOfWork.{reference}Repository.Find{reference}sBy{name}IdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys,true, "model.")});
        foreach(var {referenceVariableName} in {referenceVariableName}References)
        {{
            await Delete{reference}Async({referenceVariableName});
        }}

";
            }
            servicesAssignment = servicesAssignment[..^1];
            cascadeDelete = cascadeDelete.Length > 0 ? cascadeDelete[..^2]:"";
            deleteMethods += $@"
    private async Task Delete{name}Async({name} model)
    {{
{cascadeDelete}
        await _{variableName}Service.Delete{name}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true, "model.")});
    }}

    public async Task Delete{name}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false, variableName)})
    {{
        var model = await _unitOfWork.{name}Repository.Find{name}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true, variableName)});
{cascadeDelete}
        await _{variableName}Service.Delete{name}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true, variableName)});
    }}";
        }
        using var stream =
            new StreamWriter(
                File.Create($"{generatorVariables.ProjectDirectory}/Application/Services/CascadeDeleteService.cs"));
        stream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace};
using {NamespaceNames.ModelsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace};

namespace {generatorVariables.ProjectName}.Application.Services;
public class CascadeDeleteService 
{{
    private readonly IUnitOfWork _unitOfWork;
{servicesDeclaration}
    public CascadeDeleteService(IUnitOfWork unitOfWork{servicesArguments})
    {{
        _unitOfWork = unitOfWork;
{servicesAssignment}
    }}
{deleteMethods}
}}
");
    }

    private static void GenerateNotFoundException(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Domain/Exceptions");

        using var stream =
            new StreamWriter(
                File.Create($"{generatorVariables.ProjectDirectory}/Domain/Exceptions/NotFoundException.cs"));
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
        stream.WriteLine("\t\tservices.AddTransient<CascadeDeleteService>();");
        foreach (var modelName in modelNames.Where(modelName =>
                     File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{modelName}.cs")))
            stream.WriteLine($"\t\tservices.AddTransient<I{modelName}Service, {modelName}Service>();");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}