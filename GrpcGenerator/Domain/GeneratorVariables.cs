namespace GrpcGenerator.Domain;

public class GeneratorVariables
{
    public GeneratorVariables(DatabaseConnection databaseConnection, string projectName, string solutionName,
        string projectDirectory)
    {
        DatabaseConnection = databaseConnection;
        ProjectName = projectName;
        SolutionName = solutionName;
        ProjectDirectory = projectDirectory;
    }

    public DatabaseConnection DatabaseConnection { get; set; }
    public string ProjectName { get; set; }
    public string SolutionName { get; set; }
    public string ProjectDirectory { get; set; }
}