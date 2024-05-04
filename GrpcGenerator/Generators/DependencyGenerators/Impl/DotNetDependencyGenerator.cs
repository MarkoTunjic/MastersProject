using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.DependencyGenerators.Impl;

public class DotNetDependencyGenerator : IDependencyGenerator
{
    private static readonly Dictionary<string, string> DatabaseDependency = new()
    {
        {
            "postgres", "      <PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" Version=\"6.0.22\" />"
        },
        {
            "sqlserver", "      <PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" Version=\"6.0.25\" />"
        }
    };
    private static readonly Dictionary<string, string> ArchitectureDependencies = new()
    {
        {
            "grpc", @"    <Protobuf Include=""Protos/protofile.proto"" />
      <PackageReference Include=""Grpc.AspNetCore"" Version=""2.60.0"" />
      <PackageReference Include=""Grpc.Tools"" Version=""2.60.0"">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      </PackageReference>" 
        },
        {
            "rest", "      <PackageReference Include=\"Swashbuckle.AspNetCore\" Version=\"6.5.0\" />"
        }
    };
    
    public void GenerateDependencies(string uuid, string pathToDependencyFile)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var lines = new List<string>(File.ReadLines(pathToDependencyFile));
        
        lines.Insert(9,DatabaseDependency[generatorVariables.DatabaseProvider]);
        
        generatorVariables.Architecture.ForEach(architecture=>lines.Insert(9,ArchitectureDependencies[architecture]));
        
        File.WriteAllLines(pathToDependencyFile, lines);
    }
}