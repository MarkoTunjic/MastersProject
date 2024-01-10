using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.AdditionalActions.Impl;

public class RegisterServicesAdditionalAction : IAdditionalAction
{
    public void DoAdditionalAction(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var lines = new List<string>(File.ReadLines(generatorVariables.ProjectDirectory + "/Program.cs"));
        lines.Insert(0, $"using {generatorVariables.ProjectName}.{NamespaceNames.MappersNamespace};");
        lines.Insert(1, $"using {generatorVariables.ProjectName}.Domain;");
        lines.Insert(3, "builder.Services.AddMappers();");
        lines.Insert(4, "builder.Services.AddModels(builder.Configuration);");
        File.WriteAllLines(generatorVariables.ProjectDirectory + "/Program.cs", lines);
    }
}