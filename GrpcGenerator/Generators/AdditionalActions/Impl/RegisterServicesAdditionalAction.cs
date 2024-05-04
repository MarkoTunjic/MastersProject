using System.Runtime.InteropServices;
using GrpcGenerator.Domain;
using GrpcGenerator.Utils;

namespace GrpcGenerator.Generators.AdditionalActions.Impl;

public class RegisterServicesAdditionalAction : IAdditionalAction
{
    private static readonly Dictionary<string, string> PresentationAppRegistration = new()
    {
        {
            "grpc", "app.AddGrpcPresentation();"
        },
        {
            "rest", "app.AddRestPresentation();"
        }
    };
    
    private static readonly Dictionary<string, string> PresentationServiceRegistration = new()
    {
        {
            "grpc", "builder.Services.AddGrpcPresentation();"
        },
        {
            "rest", "builder.Services.AddRestPresentation();"
        }
    };
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
");
        
       generatorVariables.Architecture.ForEach(architecture =>
       {
           lines.Insert(2, PresentationServiceRegistration[architecture]);
           lines.Insert(4+generatorVariables.Architecture.Count,PresentationAppRegistration[architecture]);
       }); 
        
        File.WriteAllLines(generatorVariables.ProjectDirectory + "/Program.cs", lines);
    }
}