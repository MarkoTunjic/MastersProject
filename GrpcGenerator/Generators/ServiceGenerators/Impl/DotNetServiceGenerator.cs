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
            $"Server={generatorVariables.DatabaseConnection.DatabaseServer};Port={generatorVariables.DatabaseConnection.DatabasePort};Database={generatorVariables.DatabaseConnection.DatabaseName};Uid={generatorVariables.DatabaseConnection.DatabaseUid};Pwd={generatorVariables.DatabaseConnection.DatabasePwd};",
            (modelName, primaryKeys) => GenerateService(uuid, modelName, primaryKeys, targetDirectory));
        GenerateApplicationServiceRegistration(uuid, modelNames);
    }

    public void GenerateService(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        string targetDirectory)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var createMethod = GetCreateMethodCode(modelName);
        var deleteMethod = GetDeleteMethodCode(modelName, primaryKeys);
        var readAllMethod = GetFindAllMethodCode(modelName);
        var findById = GetFindByIdMethodCode(modelName, primaryKeys);
        var updateMethod = GetUpdateMethodCode(modelName);
        using var interfaceStream = new StreamWriter(File.Create($"{targetDirectory}/I{modelName}Service.cs"));
        interfaceStream.Write($@"using {NamespaceNames.ModelsNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace};
public interface I{modelName}Service
{{
    {createMethod[..createMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {deleteMethod[..deleteMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {readAllMethod[..readAllMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {findById[..findById.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {updateMethod[..updateMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
}}
");
        using var classStream = new StreamWriter(File.Create($"{targetDirectory}/Impl/{modelName}Service.cs"));
        classStream.Write($@"using {NamespaceNames.ModelsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.ServicesNamespace}.Impl;
public class {modelName}Service : I{modelName}Service
{{
    private readonly IUnitOfWork _unitOfWork;
    
    public {modelName}Service(IUnitOfWork unitOfWork)
    {{
        this._unitOfWork = unitOfWork;
    }}
    
    {createMethod}

    {deleteMethod}

    {readAllMethod}

    {findById}

    {updateMethod}
}}
");
    }

    public string GetCreateMethodCode(string modelName)
    {
        return $@"public async Task<{modelName}> Create{modelName}Async({modelName} new{modelName})
    {{
        return await _unitOfWork.{modelName}Repository.Create{modelName}Async(new{modelName});
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
        return $@"public async Task<List<{modelName}>> FindAll{modelName}Async()
    {{
        return await _unitOfWork.{modelName}Repository.FindAll{modelName}Async();
    }}";
    }

    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return
            $@"public async Task<{modelName}?> Find{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        return await _unitOfWork.{modelName}Repository.Find{modelName}ByIdAsync({DatabaseSchemaUtils.GetMethodInputForPrimaryKeys(primaryKeys, true)});
    }}";
    }

    public string GetUpdateMethodCode(string modelName)
    {
        return $@"public async Task Update{modelName}Async({modelName} updated{modelName})
    {{
        await _unitOfWork.{modelName}Repository.Update{modelName}Async(updated{modelName});
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
        foreach (var modelName in modelNames)
            stream.WriteLine($"\t\tservices.AddTransient<I{modelName}Service, {modelName}Service>();");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}