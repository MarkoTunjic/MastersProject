using GrpcGenerator.Utils;
using Microsoft.Extensions.Configuration;

var file = new FileInfo("../../../appsettings.json");

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile(file.DirectoryName + "/" + file.Name)
    .Build();

var guid = Guid.NewGuid().ToString();
Copier.CopyDirectory(config["sourceCodeRoot"] + "/templates/dotnet6/Template",
    config["sourceCodeRoot"] + "/" + guid + "/Template");
ProjectRenamer.RenameDotNetProject(config["sourceCodeRoot"] + "/" + guid, "Template", "Template",
    "FirstSolution", "FirstProject");
Zipper.ZipDirectory(config["sourceCodeRoot"] + "/" + guid,
    config["sourceCodeRoot"] + "/" + config["mainProjectName"] + "/" + "FirstSolution.zip");
Directory.Delete(config["sourceCodeRoot"] + "/" + guid, true);