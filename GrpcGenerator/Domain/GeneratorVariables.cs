namespace GrpcGenerator.Domain;

public class GeneratorVariables
{
    public GeneratorVariables(DatabaseConnection databaseConnection, string projectName, string solutionName)
    {
        DatabaseConnection = databaseConnection;
        ProjectName = projectName;
        SolutionName = solutionName;
    }

    public DatabaseConnection DatabaseConnection { get; set; }
    public string ProjectName { get; set; }
    public string SolutionName { get; set; }
}