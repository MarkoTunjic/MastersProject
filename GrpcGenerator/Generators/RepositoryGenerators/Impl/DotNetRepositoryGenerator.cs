using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.RepositoryGenerators.Impl;

public class DotNetRepositoryGenerator : IRepositoryGenerator
{
    public void GenerateRepositories(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);

        var targetDirectory = $"{generatorVariables.ProjectDirectory}/Infrastructure/Repositories";

        var crudOperations = File.ReadAllText($"{targetDirectory}/Common/CrudOperations.cs");
        crudOperations = crudOperations.Replace("{ProjectName}", generatorVariables.ProjectName);
        File.WriteAllText($"{targetDirectory}/Common/CrudOperations.cs", crudOperations);

        Directory.CreateDirectory($"{targetDirectory}/Impl");

        var modelNames = DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid, "postgres",
            $"Server={generatorVariables.DatabaseConnection.DatabaseServer};Port={generatorVariables.DatabaseConnection.DatabasePort};Database={generatorVariables.DatabaseConnection.DatabaseName};Uid={generatorVariables.DatabaseConnection.DatabaseUid};Pwd={generatorVariables.DatabaseConnection.DatabasePwd};",
            (modelName, primaryKeys) => GenerateRepository(uuid, modelName, primaryKeys, targetDirectory));
        GenerateUnitOfWork(uuid, modelNames);
        GenerateInfrastructureServiceRegistration(uuid, modelNames);
    }

    public void GenerateRepository(string uuid, string modelName, Dictionary<string, Type> primaryKeys,
        string targetDirectory)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var createMethod = GetCreateMethodCode(modelName);
        var deleteMethod = GetDeleteMethodCode(modelName, primaryKeys);
        var readAllMethod = GetFindAllMethodCode(modelName);
        var findById = GetFindByIdMethodCode(modelName, primaryKeys);
        var updateMethod = GetUpdateMethodCode(modelName);
        using var interfaceStream = new StreamWriter(File.Create($"{targetDirectory}/I{modelName}Repository.cs"));
        interfaceStream.Write($@"using {NamespaceNames.ModelsNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace};
public interface I{modelName}Repository
{{
    {createMethod[..createMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {deleteMethod[..deleteMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {readAllMethod[..readAllMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {findById[..findById.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
    {updateMethod[..updateMethod.IndexOf("\n", StringComparison.Ordinal)].Replace(" async", "")};
}}
");
        using var classStream = new StreamWriter(File.Create($"{targetDirectory}/Impl/{modelName}Repository.cs"));
        classStream.Write($@"using {NamespaceNames.ModelsNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace}.Common;

namespace {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace}.Impl;
public class {modelName}Repository : I{modelName}Repository
{{
    private readonly {generatorVariables.DatabaseConnection.DatabaseName}Context _dbContext;
    
    public {modelName}Repository({generatorVariables.DatabaseConnection.DatabaseName}Context dbContext)
    {{
        this._dbContext = dbContext;
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
        return $@"public async Task<{modelName}> CreateAsync{modelName}({modelName} new{modelName})
    {{
        return await CrudOperations.CreateAsync(new{modelName}, _dbContext);
    }}";
    }

    public string GetDeleteMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return $@"public async Task Delete{modelName}ByIdAsync({GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        var tbd = await Find{modelName}ByIdAsync({GetMethodInputForPrimaryKeys(primaryKeys, true)});
        if(tbd == null)
        {{
            return;
        }}
        await CrudOperations.DeleteAsync(tbd, _dbContext);
    }}";
    }

    public string GetFindAllMethodCode(string modelName)
    {
        return $@"public async Task<List<{modelName}>> FindAllAsync{modelName}()
    {{
        return await CrudOperations.FindAllAsync<{modelName}>(_dbContext);
    }}";
    }

    public string GetFindByIdMethodCode(string modelName, Dictionary<string, Type> primaryKeys)
    {
        return
            $@"public async Task<{modelName}?> Find{modelName}ByIdAsync({GetMethodInputForPrimaryKeys(primaryKeys, false)})
    {{
        return await CrudOperations.FindByIdAsync<{modelName}>(_dbContext, {GetMethodInputForPrimaryKeys(primaryKeys, true)});
    }}";
    }

    public string GetUpdateMethodCode(string modelName)
    {
        return $@"public async Task Update{modelName}({modelName} updated{modelName})
    {{
        await CrudOperations.UpdateAsync(updated{modelName}, _dbContext);
    }}
";
    }

    private static string GetMethodInputForPrimaryKeys(IReadOnlyDictionary<string, Type> primaryKeys, bool call)
    {
        var result = "";
        var i = 0;
        foreach (var entry in primaryKeys)
        {
            if (i != 0) result += ", ";
            result += $"{(call ? "" : entry.Value + " ")}{char.ToLower(entry.Key[0]) + entry.Key[1..]}";
            i++;
        }

        return result;
    }

    private static void GenerateUnitOfWork(string uuid, List<string> modelNames)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Infrastructure/Utils");
        using var interfaceStream =
            new StreamWriter(
                File.Create($"{generatorVariables.ProjectDirectory}/Infrastructure/Utils/IUnitOfWork.cs"));
        interfaceStream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace};
public interface IUnitOfWork
{{
");
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Infrastructure/Utils/Impl");
        using var classStream =
            new StreamWriter(File.Create(
                $"{generatorVariables.ProjectDirectory}/Infrastructure/Utils/Impl/DependencyInjectionUnitOfWork.cs"));
        classStream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace};

namespace {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace}.Impl;
public class DependencyInjectionUnitOfWork : IUnitOfWork
{{
    public DependencyInjectionUnitOfWork(");
        var i = 0;
        foreach (var modelName in modelNames)
        {
            interfaceStream.WriteLine($"\tpublic I{modelName}Repository {modelName}Repository {{ get; }}\n");
            if (i != 0)
                classStream.Write(", ");
            classStream.Write($"I{modelName}Repository {char.ToLower(modelName[0]) + modelName[1..]}Repository");
            i++;
        }

        classStream.Write(")");
        classStream.WriteLine("\n\t{");
        interfaceStream.Write("}");

        foreach (var modelName in modelNames)
            classStream.WriteLine(
                $"\t\t{modelName}Repository = {char.ToLower(modelName[0]) + modelName[1..]}Repository;");
        classStream.WriteLine("\t}");
        foreach (var modelName in modelNames)
            classStream.WriteLine($"\tpublic I{modelName}Repository {modelName}Repository {{ get; }}\n");
        classStream.WriteLine("}");
    }

    private static void GenerateInfrastructureServiceRegistration(string uuid, List<string> modelNames)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        using var stream =
            new StreamWriter(
                File.Create(
                    $"{generatorVariables.ProjectDirectory}/Infrastructure/InfrastructureServiceRegistration.cs"));
        stream.Write($@"using {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace};
using {generatorVariables.ProjectName}.{NamespaceNames.RepositoryNamespace}.Impl;
using {generatorVariables.ProjectName}.{NamespaceNames.UnitOfWorkNamespace}.Impl;

namespace {generatorVariables.ProjectName}.Infrastructure;
public static class InfrastructureServiceRegistration
{{
    public static void AddInfrastructure(this IServiceCollection services)
    {{
        services.AddTransient<IUnitOfWork,DependencyInjectionUnitOfWork>();
");
        foreach (var modelName in modelNames)
            stream.WriteLine($"\t\tservices.AddTransient<I{modelName}Repository, {modelName}Repository>();");
        stream.WriteLine("\t}");
        stream.WriteLine("}");
    }
}