using Microsoft.Extensions.Configuration;

var file = new FileInfo("../../../appsettings.json");

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile(file.DirectoryName + "/" + file.Name)
    .Build();