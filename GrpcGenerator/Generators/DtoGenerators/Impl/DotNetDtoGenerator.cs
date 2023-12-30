namespace GrpcGenerator.Generators.DtoGenerators.Impl;

public class DotNetDtoGenerator : IDtoGenerator
{
    public void GenerateDtos(string pathToModels, string destinationDirectory, string databaseName, string packageName)
    {
        foreach (var file in Directory.EnumerateFiles(pathToModels))
        {
            if (file.EndsWith($"{databaseName}Context.cs"))
                continue;
            var className =
                $"{file[(file.LastIndexOf("/", StringComparison.Ordinal) + 1)..file.LastIndexOf(".", StringComparison.Ordinal)]}Dto";
            using var stream = new StreamWriter(File.Create($"{destinationDirectory}/{className}.cs"));
            stream.WriteLine($"namespace {packageName};");
            stream.WriteLine($"\npublic class {className} {{");
            var variables = File.ReadLines(file).Where(line =>
                line.Contains("public ") && !line.Contains("class ") && !line.Contains("(") &&
                !line.Contains("virtual "));
            foreach (var variable in variables) stream.WriteLine($"\t{variable.Trim()}");
            stream.WriteLine("}");
        }
    }
}