using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.DependencyGenerators.Impl;

public class DotNetDependencyGenerator : IDependencyGenerator
{
    public void GenerateDependencies(string uuid, string pathToDependencyFile)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var lines = new List<string>(File.ReadLines(pathToDependencyFile));
        
        if (generatorVariables.DatabaseProvider == "postgres")
        {
            lines.Insert(9,
                "      <PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" Version=\"6.0.22\" />");
        }

        if (generatorVariables.DatabaseProvider == "sqlserver")
        {
            lines.Insert(9,"      <PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" Version=\"6.0.25\" />");
        }
        if (generatorVariables.Architecture == "grpc")
        {
            lines.Insert(9, @"    <ItemGroup>
        <Protobuf Include=""Protos/protofile.proto"" />
    </ItemGroup>");
            lines.Insert(9,
                @"      <PackageReference Include=""Grpc.AspNetCore"" Version=""2.60.0"" />
      <PackageReference Include=""Grpc.Tools"" Version=""2.60.0"">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      </PackageReference>");
        }

        if (generatorVariables.Architecture.Equals("rest"))
        {
            lines.Insert(9, "      <PackageReference Include=\"Swashbuckle.AspNetCore\" Version=\"6.5.0\" />");
        }
        
        File.WriteAllLines(pathToDependencyFile, lines);
    }
}