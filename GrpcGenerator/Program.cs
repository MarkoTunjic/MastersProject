using Microsoft.Extensions.Configuration;

IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();