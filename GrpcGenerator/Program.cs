using GrpcGenerator.Generators.DtoGenerators;
using GrpcGenerator.Generators.DtoGenerators.Impl;
using GrpcGenerator.Generators.MapperGenerators;
using GrpcGenerator.Generators.MapperGenerators.Impl;
using GrpcGenerator.Generators.ModelGenerators;
using GrpcGenerator.Generators.ModelGenerators.Impl;
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

ProjectRenamer.RenameDotNetProject($"{config["sourceCodeRoot"]}/{guid}", oldSolutionName, oldProjectName,
    newSolutionName, newProjectName);

IModelGenerator modelGenerator = new EfCoreModelGenerator();
Directory.CreateDirectory($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain");

var databaseName = "zavrsni_rad";
var databaseServer = "127.0.0.1";
var databasePort = "5432";
var databaseUid = "postgres";
var databasePwd = "bazepodataka";

ServicesProvider.SetServices(services.BuildServiceProvider());
modelGenerator.GenerateModels(
    $"Server={databaseServer};Port={databasePort};Database={databaseName};Uid={databaseUid};Pwd={databasePwd};",
    "postgres", $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain");

IDtoGenerator dtoGenerator = new DotNetDtoGenerator();
Directory.CreateDirectory($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain/Dto");
dtoGenerator.GenerateDtos($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain/Models",
    $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain/Dto", databaseName,
    $"{newProjectName}.Domain.Dto");

IMapperGenerator mapperGenerator = new DotnetMapperGenerator();
Directory.CreateDirectory($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain/Mappers");
mapperGenerator.GenerateMappers($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain/Dto",
    $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/Domain/Mappers",
    $"{newProjectName}.Domain.Mappers",
    "Domain.Models", $"{newProjectName}.Domain.Dto");

Zipper.ZipDirectory($"{config["sourceCodeRoot"]}/{guid}",
    $"{config["sourceCodeRoot"]}/{config["mainProjectName"]}/FirstSolution.zip");

Directory.Delete($"{config["sourceCodeRoot"]}/{guid}", true);