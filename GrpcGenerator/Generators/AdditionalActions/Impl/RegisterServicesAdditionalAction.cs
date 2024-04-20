using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.AdditionalActions.Impl;

public class RegisterServicesAdditionalAction : IAdditionalAction
{
    public void DoAdditionalAction(string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var lines = new List<string>(File.ReadLines(generatorVariables.ProjectDirectory + "/Program.cs"));
        lines.RemoveAt(3);
        
        lines.Insert(0, $@"using {generatorVariables.ProjectName}.{NamespaceNames.MappersNamespace};
using {generatorVariables.ProjectName}.Domain;
using {generatorVariables.ProjectName}.Infrastructure;
using {generatorVariables.ProjectName}.Application;
using {generatorVariables.ProjectName}.Presentation;
");

        lines.Insert(2, @"builder.Services.AddMappers();
builder.Services.AddModels(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddApplication();
builder.Services.AddPresentation();
");

        if (generatorVariables.Architecture == "rest")
        {
            lines.Insert(4,"app.AddPresentation();");
        }
        
        File.WriteAllLines(generatorVariables.ProjectDirectory + "/Program.cs", lines);
    }
}