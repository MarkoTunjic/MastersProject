namespace GrpcGenerator.Domain;

public class GeneratorVariables
{
    public GeneratorVariables(DatabaseConnectionData databaseConnectionData, string projectName, string solutionName,
        string projectDirectory, string databaseProvider, List<string> architecture, List<string> includedTables)
    {
        DatabaseConnectionData = databaseConnectionData;
        ProjectName = projectName;
        SolutionName = solutionName;
        ProjectDirectory = projectDirectory;
        DatabaseProvider = databaseProvider;
        Architecture = architecture;
        IncludedTables = includedTables;
    }

    public DatabaseConnectionData DatabaseConnectionData { get; set; }
    public string ProjectName { get; set; }
    public string SolutionName { get; set; }
    public string ProjectDirectory { get; set; }
    public string DatabaseProvider { get; set; }
    public List<string> Architecture { get; set; }
    public List<string>? IncludedTables { get; set; }
}