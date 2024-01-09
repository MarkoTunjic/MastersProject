using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.DtoGenerators.Impl;

public class DotNetDtoGenerator : IDtoGenerator
{
    public void GenerateDtos(string uuid, string packageName)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var destinationDirectory = $"{generatorVariables.ProjectDirectory}/Domain/Dto";
        Directory.CreateDirectory(destinationDirectory);
        var pathToModels = $"{generatorVariables.ProjectDirectory}/Domain/Models";
        foreach (var file in Directory.EnumerateFiles(pathToModels))
        {
            if (file.EndsWith($"{generatorVariables.DatabaseConnection.DatabaseName}Context.cs"))
                continue;
            var className =
                $"{file[(file.LastIndexOf("/", StringComparison.Ordinal) + 1)..file.LastIndexOf(".", StringComparison.Ordinal)]}Dto";
            using var stream = new StreamWriter(File.Create($"{destinationDirectory}/{className}.cs"));
            stream.WriteLine($"namespace {packageName};");
            stream.WriteLine($"\npublic class {className} \n{{");
            var variables = File.ReadLines(file).Where(line =>
                line.Contains("public ") && !line.Contains("class ") && !line.Contains("(") &&
                !line.Contains("virtual "));
            foreach (var variable in variables) stream.WriteLine($"\t{variable.Trim()}");
            stream.WriteLine("}");
        }
    }
}