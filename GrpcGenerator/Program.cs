using GrpcGenerator.Generators.ModelGenerators;
using GrpcGenerator.Generators.ModelGenerators.Impl;
using GrpcGenerator.Utils;
using Microsoft.Extensions.Configuration;

var file = new FileInfo("../../../appsettings.json");

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile(file.DirectoryName + "/" + file.Name)
    .Build();

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
Directory.CreateDirectory($"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/domain");
modelGenerator.GenerateModels("Server=127.0.0.1;Port=5432;Database=zavrsni_rad;Uid=postgres;Pwd=bazepodataka;",
    "postgres", $"{config["sourceCodeRoot"]}/{guid}/{newSolutionName}/{newProjectName}/domain");

Zipper.ZipDirectory($"{config["sourceCodeRoot"]}/{guid}",
    $"{config["sourceCodeRoot"]}/{config["mainProjectName"]}/FirstSolution.zip");

Directory.Delete($"{config["sourceCodeRoot"]}/{guid}", true);