namespace GrpcGenerator.Utils;

public class ProjectRenamer
{
    public static void RenameDotNetProject(string solutionLocation, string oldSolutionName, string oldProjectName,
        string uuid)
    {
        var generatorVariables = GeneratorVariablesProvider.GetVariables(uuid);
        var newSolutionName = generatorVariables.SolutionName;
        var newProjectName = generatorVariables.ProjectName;
        Directory.Move(
            solutionLocation + "/" + oldSolutionName + "/" + oldProjectName + "/" + oldProjectName + ".csproj",
            solutionLocation + "/" + oldSolutionName + "/" + oldProjectName + "/" + newProjectName + ".csproj");
        Directory.Move(solutionLocation + "/" + oldSolutionName + "/" + oldProjectName,
            solutionLocation + "/" + oldSolutionName + "/" + newProjectName);
        var text = File.ReadAllText(solutionLocation + "/" + oldSolutionName + "/" + oldSolutionName + ".sln");
        text = text.Replace("Template", newProjectName);
        File.WriteAllText(solutionLocation + "/" + oldSolutionName + "/" + oldSolutionName + ".sln", text);
        Directory.Move(solutionLocation + "/" + oldSolutionName + "/" + oldSolutionName + ".sln",
            solutionLocation + "/" + oldSolutionName + "/" + newSolutionName + ".sln");
        Directory.Move(solutionLocation + "/" + oldSolutionName, solutionLocation + "/" + newSolutionName);
    }
}