using GrpcGenerator.zipper;
using Microsoft.Extensions.Configuration;

FileInfo file = new FileInfo("../../../appsettings.json");
IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile(file.DirectoryName + "/" + file.Name)
    .Build();