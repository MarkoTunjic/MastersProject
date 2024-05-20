using GrpcGenerator.Domain;
using GrpcGenerator.Generators.AdditionalActions;
using GrpcGenerator.Generators.AdditionalActions.Impl;
using GrpcGenerator.Generators.ConfigGenerators;
using GrpcGenerator.Generators.ConfigGenerators.Impl;
using GrpcGenerator.Generators.DependencyGenerators;
using GrpcGenerator.Generators.DependencyGenerators.Impl;
using GrpcGenerator.Generators.DtoGenerators;
using GrpcGenerator.Generators.DtoGenerators.Impl;
using GrpcGenerator.Generators.MapperGenerators;
using GrpcGenerator.Generators.MapperGenerators.Impl;
using GrpcGenerator.Generators.ModelGenerators;
using GrpcGenerator.Generators.ModelGenerators.Impl;
using GrpcGenerator.Generators.PresentationGenerators;
using GrpcGenerator.Generators.PresentationGenerators.Impl.Grpc.DotNet;
using GrpcGenerator.Generators.PresentationGenerators.Impl.Rest.DotNet;
using GrpcGenerator.Generators.RepositoryGenerators;
using GrpcGenerator.Generators.RepositoryGenerators.Impl;
using GrpcGenerator.Generators.ServiceGenerators;
using GrpcGenerator.Generators.ServiceGenerators.Impl;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Application.Services.Impl;

public class GeneratorServiceImpl : IGeneratorService
{
    private readonly IConfiguration _configuration;
    private static readonly Dictionary<string, IPresentationGenerator> PresentationGenerators = new()
    {
        {
            "grpc", new DotNetGrpcPresentationGenerator() 
        },
        {
            "rest", new DotnetRestGenerator()
        }
    };
    public GeneratorServiceImpl(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public byte[] GetZipProject(GenerationRequest generationRequest)
    {
        var guid = Guid.NewGuid().ToString();
        byte[] result;
        try
        {
            var oldSolutionName = _configuration["oldSolutionName"]!;
            var oldProjectName = _configuration["oldProjectName"]!;

            Copier.CopyDirectory($"{_configuration["sourceCodeRoot"]}/templates/dotnet6/{oldSolutionName}",
                $"{_configuration["sourceCodeRoot"]}/{guid}/{oldSolutionName}");

            var projectRoot =
                $"{_configuration["sourceCodeRoot"]}/{guid}/{generationRequest.SolutionName}/{generationRequest.ProjectName}";
            var generatorVariables =
                new GeneratorVariables(
                    new DatabaseConnectionData(generationRequest.DatabaseConnectionData.DatabaseServer,
                        generationRequest.DatabaseConnectionData.DatabaseName,
                        generationRequest.DatabaseConnectionData.DatabasePort,
                        generationRequest.DatabaseConnectionData.DatabasePwd,
                        generationRequest.DatabaseConnectionData.DatabaseUid,
                        generationRequest.DatabaseConnectionData.Provider),
                    generationRequest.ProjectName, generationRequest.SolutionName, projectRoot,
                    generationRequest.DatabaseConnectionData.Provider, generationRequest.Architectures,
                    generationRequest.IncludedTables, generationRequest.Cascade);
            GeneratorVariablesProvider.AddVariables(guid, generatorVariables);

            ProjectRenamer.RenameDotNetProject($"{_configuration["sourceCodeRoot"]}/{guid}", oldSolutionName,
                oldProjectName,
                guid);
            IDependencyGenerator dependencyGenerator = new DotNetDependencyGenerator();
            dependencyGenerator.GenerateDependencies(guid,
                $"{_configuration["sourceCodeRoot"]}/{guid}/{generationRequest.SolutionName}/{generationRequest.ProjectName}/{generationRequest.ProjectName}.csproj");

            IConfigGenerator databaseConfigGenerator = new DotNetDatabaseConfigGenerator();
            databaseConfigGenerator.GenerateConfig(
                $"{_configuration["sourceCodeRoot"]}/{guid}/{generationRequest.SolutionName}/{generationRequest.ProjectName}",
                guid);

            IModelGenerator modelGenerator = new EfCoreModelGenerator();
            modelGenerator.GenerateModels(guid);

            IDtoGenerator dtoGenerator = new DotNetDtoGenerator();
            dtoGenerator.GenerateDtos(guid);

            IMapperGenerator mapperGenerator = new DotnetMapperGenerator();
            mapperGenerator.GenerateMappers(guid);

            IAdditionalAction registerServices = new RegisterServicesAdditionalAction();
            registerServices.DoAdditionalAction(guid);

            IRepositoryGenerator repositoryGenerator = new DotNetRepositoryGenerator();
            repositoryGenerator.GenerateRepositories(guid);

            IServiceGenerator serviceGenerator = new DotNetServiceGenerator();
            serviceGenerator.GenerateServices(guid);

            generationRequest.Architectures.ForEach(architecture =>
                PresentationGenerators[architecture].GeneratePresentation(guid));

            Directory.CreateDirectory($"{_configuration["sourceCodeRoot"]}/{_configuration["mainProjectName"]}/{guid}");
            Zipper.ZipDirectory($"{_configuration["sourceCodeRoot"]}/{guid}",
                $"{_configuration["sourceCodeRoot"]}/{_configuration["mainProjectName"]}/{guid}/{generationRequest.SolutionName}.zip");
            result = File.ReadAllBytes(
                $"{_configuration["sourceCodeRoot"]}/{_configuration["mainProjectName"]}/{guid}/{generationRequest.SolutionName}.zip");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            throw new Exception(e.Message);
        }
        finally
        {
            GeneratorVariablesProvider.RemoveVariables(guid);
            Directory.Delete($"{_configuration["sourceCodeRoot"]}/{guid}", true);
        }

        return result;
    }

    public List<string> GetAvailableTables(DatabaseConnectionData databaseConnectionData)
    {
        var guid = Guid.NewGuid().ToString();

        return DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(guid, databaseConnectionData.Provider,
            databaseConnectionData.ToConnectionString(), null, (_) => false);
    }
}