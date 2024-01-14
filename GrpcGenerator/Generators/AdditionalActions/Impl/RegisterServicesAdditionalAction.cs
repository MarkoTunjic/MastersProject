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
        lines.Insert(2, $"using {generatorVariables.ProjectName}.Infrastructure;");
        lines.Insert(3, $"using {generatorVariables.ProjectName}.Application;");

        lines.Insert(5, "builder.Services.AddMappers();");
        lines.Insert(6, "builder.Services.AddModels(builder.Configuration);");
        lines.Insert(7, "builder.Services.AddInfrastructure();");
        lines.Insert(8, "builder.Services.AddApplication();");


        File.WriteAllLines(generatorVariables.ProjectDirectory + "/Program.cs", lines);
    }
}