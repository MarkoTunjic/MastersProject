namespace GrpcGenerator.Generators.DependencyGenerators.Impl;

public class DotNetDependencyGenerator : IDependencyGenerator
{
    public void GenerateDependencies(string pathToDependencyFile)
    {
        var lines = new List<string>(File.ReadLines(pathToDependencyFile));
        lines.Insert(12,
            "      <PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" Version=\"6.0.22\" />\n");
        File.WriteAllLines(pathToDependencyFile, lines);
    }
}