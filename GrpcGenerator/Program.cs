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
using GrpcGenerator.Generators.PresentationGenerators.Impl.Rest.DotNet;
using GrpcGenerator.Generators.RepositoryGenerators;
using GrpcGenerator.Generators.RepositoryGenerators.Impl;
using GrpcGenerator.Generators.ServiceGenerators;
using GrpcGenerator.Generators.ServiceGenerators.Impl;
using GrpcGenerator.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var file = new FileInfo("../../../appsettings.json");
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile(file.DirectoryName + "/" + file.Name)
    .Build();

var services = new ServiceCollection();
services.AddSingleton(config);

var guid = Guid.NewGuid().ToString();

const string oldSolutionName = "Template";
const string oldProjectName = "Template";

Copier.CopyDirectory($"{config["sourceCodeRoot"]}/templates/dotnet6/{oldSolutionName}",
    $"{config["sourceCodeRoot"]}/{guid}/{oldSolutionName}");

const string newSolutionName = "FirstSolution";
const string newProjectName = "FirstProject";
var databaseName = "MastersProject";
var databaseServer = "127.0.0.1";
var databasePort = "1433";
var databaseUid = "sa";
var databasePwd = "Drvahagn10.";
var provider = "sqlserver";
var projectRoot = $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}";
var architecture = "rest";
var generatorVariables =
    new GeneratorVariables(
        new DatabaseConnection(databaseServer, databaseName, databasePort, databasePwd, databaseUid, provider),
        newProjectName, newSolutionName, projectRoot, provider, architecture);
GeneratorVariablesProvider.AddVariables(guid, generatorVariables);

ProjectRenamer.RenameDotNetProject($"{config["sourceCodeRoot"]}/{guid}", oldSolutionName, oldProjectName,
    guid);

ServicesProvider.SetServices(services.BuildServiceProvider());

IDependencyGenerator dependencyGenerator = new DotNetDependencyGenerator();
dependencyGenerator.GenerateDependencies(guid,
    $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/{newProjectName}.csproj");

IConfigGenerator databaseConfigGenerator = new DotNetDatabaseConfigGenerator();
databaseConfigGenerator.GenerateConfig($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}", guid);

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

IPresentationGenerator presentationGenerator = new DotnetRestGenerator();
presentationGenerator.GeneratePresentation(guid);

Zipper.ZipDirectory($"{config["sourceCodeRoot"]}/{guid}",
    $"{config["sourceCodeRoot"]}/{config["mainProjectName"]}/FirstSolution.zip");

Directory.Delete($"{config["sourceCodeRoot"]}/{guid}", true);