using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.DtoGenerators.Impl;

public class DotNetDtoGenerator : IDtoGenerator
{
    public void GenerateDtos(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var packageName = $"{generatorVariables.ProjectName}.{NamespaceNames.DtoNamespace}";
        var destinationDirectory = $"{generatorVariables.ProjectDirectory}/Domain/Dto";
        Directory.CreateDirectory(destinationDirectory);
        Directory.CreateDirectory($"{generatorVariables.ProjectDirectory}/Domain/Request");

        DatabaseSchemaUtils.FindTablesAndExecuteActionForEachTable(uuid,
                    generatorVariables.DatabaseProvider,
            generatorVariables.DatabaseConnection.ToConnectionString(),
            (tableName, primaryKeys, foreignKeys) => GenerateDto(uuid,tableName,primaryKeys,foreignKeys,destinationDirectory,packageName));
        
        /*foreach (var file in Directory.EnumerateFiles(pathToModels))
        {
            if (file.EndsWith($"{generatorVariables.DatabaseConnection.DatabaseName}Context.cs"))
                continue;
            var className =
                $"{file[(file.LastIndexOf("/", StringComparison.Ordinal) + 1)..file.LastIndexOf(".", StringComparison.Ordinal)]}";
            using var dtoStream = new StreamWriter(File.Create($"{destinationDirectory}/{className}Dto.cs"));
            using var requestStream = new StreamWriter(File.Create($"{generatorVariables.ProjectDirectory}/Domain/Request/{className}WriteDto.cs"));
            
            dtoStream.WriteLine($"namespace {packageName};");
            dtoStream.WriteLine($"\npublic class {className}Dto \n{{");
            
            requestStream.WriteLine($"namespace {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};");
            requestStream.WriteLine($"\npublic class {className}WriteDto \n{{");

            var foreignKeys = DatabaseSchemaUtils.GetForeignKeys(uuid, className);
            var variables = File.ReadLines(file).Where(line =>
                line.Contains("public ") && !line.Contains("class ") && !line.Contains("(") &&
                !line.Contains("virtual "));

            var primaryKeys = DatabaseSchemaUtils.GetPrimaryKeysForModel(generatorVariables.DatabaseProvider,
                generatorVariables.DatabaseConnection.ToConnectionString(), className)
                .Select(StringUtils.GetDotnetNameFromSqlName)
                .ToList();
            foreach (var variable in variables)
            {
                dtoStream.WriteLine($"\t{variable.Trim()}");
                var variableName = variable.Split(" ")[10];
                if (primaryKeys.Contains(variableName))
                {
                    continue;
                }
                
                if (variableName.EndsWith("Id") && foreignKeys.ContainsKey(variableName[..^2]))
                {
                    continue;
                }
                requestStream.WriteLine($"\t{variable.Trim()}");
            }
            dtoStream.WriteLine("}");
            requestStream.WriteLine("}");
        }*/
    }

    private static void GenerateDto(string uuid, string className,Dictionary<string,Type> primaryKeysAndTypes,Dictionary<string,Dictionary<string,Type>> foreignKeys, string destinationDirectory, string packageName)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        if (!File.Exists($"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs"))
        {
            return;
        }

        using var dtoStream = new StreamWriter(File.Create($"{destinationDirectory}/{className}Dto.cs"));
        using var requestStream = new StreamWriter(File.Create($"{generatorVariables.ProjectDirectory}/Domain/Request/{className}WriteDto.cs"));
            
        dtoStream.WriteLine($"namespace {packageName};");
        dtoStream.WriteLine($"\npublic class {className}Dto \n{{");
            
        requestStream.WriteLine($"namespace {generatorVariables.ProjectName}.{NamespaceNames.RequestsNamespace};");
        requestStream.WriteLine($"\npublic class {className}WriteDto \n{{");

        var file = $"{generatorVariables.ProjectDirectory}/Domain/Models/{className}.cs";
        var variables = File.ReadLines(file).Where(line =>
            line.Contains("public ") && !line.Contains("class ") && !line.Contains("(") &&
            !line.Contains("virtual "));

        var primaryKeys = primaryKeysAndTypes.Keys;
        foreach (var variable in variables)
        {
            dtoStream.WriteLine($"\t{variable.Trim()}");
            var variableName = variable.Split(" ")[10];
            if (primaryKeys.Contains(variableName))
            {
                continue;
            }
                
            if (variableName.EndsWith("Id") && foreignKeys.ContainsKey(variableName[..^2]))
            {
                continue;
            }
            requestStream.WriteLine($"\t{variable.Trim()}");
        }
        dtoStream.WriteLine("}");
        requestStream.WriteLine("}");
    }
}