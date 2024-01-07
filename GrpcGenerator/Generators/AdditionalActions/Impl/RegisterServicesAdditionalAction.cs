namespace GrpcGenerator.Generators.AdditionalActions.Impl;

public class RegisterServicesAdditionalAction : IAdditionalAction
{
    public void DoAdditionalAction(string projectRoot, string projectName)
    {
        var lines = new List<string>(File.ReadLines(projectRoot + "/Program.cs"));
        lines.Insert(0, $"using {projectName}.Domain.Mappers;");
        lines.Insert(1, $"using {projectName}.Domain;");
        lines.Insert(3, "builder.Services.AddMappers();");
        lines.Insert(4, "builder.Services.AddModels(builder.Configuration);");
        File.WriteAllLines(projectRoot + "/Program.cs", lines);
    }
}