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
using GrpcGenerator.Generators.RepositoryGenerators;
using GrpcGenerator.Generators.RepositoryGenerators.Impl;
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
var databaseName = "zavrsni_rad";
var databaseServer = "127.0.0.1";
var databasePort = "5432";
var databaseUid = "postgres";
var databasePwd = "bazepodataka";
var provider = "postgres";
var projectRoot = $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}";
var generatorVariables =
    new GeneratorVariables(
        new DatabaseConnection(databaseServer, databaseName, databasePort, databasePwd, databaseUid, provider),
        newProjectName, newSolutionName, projectRoot);
GeneratorVariablesProvider.AddVariables(guid, generatorVariables);

ProjectRenamer.RenameDotNetProject($"{config["sourceCodeRoot"]}/{guid}", oldSolutionName, oldProjectName,
    guid);

ServicesProvider.SetServices(services.BuildServiceProvider());

IModelGenerator modelGenerator = new EfCoreModelGenerator();
modelGenerator.GenerateModels(guid);

IDtoGenerator dtoGenerator = new DotNetDtoGenerator();
dtoGenerator.GenerateDtos(guid);

IMapperGenerator mapperGenerator = new DotnetMapperGenerator();
mapperGenerator.GenerateMappers(guid);

IDependencyGenerator dependencyGenerator = new DotNetDependencyGenerator();
dependencyGenerator.GenerateDependencies(
    $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/{newProjectName}.csproj");

IConfigGenerator databaseConfigGenerator = new DotNetDatabaseConfigGenerator();
databaseConfigGenerator.GenerateConfig($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}", guid);

IAdditionalAction registerServices = new RegisterServicesAdditionalAction();
registerServices.DoAdditionalAction(guid);

IRepositoryGenerator repositoryGenerator = new DotNetRepositoryGenerator();
repositoryGenerator.GenerateRepositories(guid);

Zipper.ZipDirectory($"{config["sourceCodeRoot"]}/{guid}",
    $"{config["sourceCodeRoot"]}/{config["mainProjectName"]}/FirstSolution.zip");

Directory.Delete($"{config["sourceCodeRoot"]}/{guid}", true);