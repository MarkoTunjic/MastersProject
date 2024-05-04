namespace GrpcGenerator.Domain;

public class GenerationRequest
{
    public string SolutionName { get; set; } 
    public string ProjectName { get; set; } 
    public DatabaseConnectionData DatabaseConnectionData { get; set; }
    public bool Cascade { get; set; }
    public List<string> Architectures { get; set; }
    public List<string> IncludedTables { get; set; }
}