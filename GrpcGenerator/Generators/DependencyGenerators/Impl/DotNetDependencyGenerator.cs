namespace GrpcGenerator.Generators.DependencyGenerators.Impl;

public class DotNetDependencyGenerator : IDependencyGenerator
{
    public void GenerateDependencies(string pathToDependencyFile)
    {
        var lines = new List<string>(File.ReadLines(pathToDependencyFile));
        lines.Insert(8,@"    <ItemGroup>
        <Protobuf Include=""Protos/protofile.proto"" />
    </ItemGroup>");
        lines.Insert(12,
            "      <PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" Version=\"6.0.22\" />");
        lines.Insert(12,
            @"      <PackageReference Include=""Grpc.AspNetCore"" Version=""2.60.0"" />
      <PackageReference Include=""Grpc.Tools"" Version=""2.60.0"">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      </PackageReference>");
        File.WriteAllLines(pathToDependencyFile, lines);
    }
}