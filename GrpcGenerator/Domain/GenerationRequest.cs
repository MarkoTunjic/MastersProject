namespace GrpcGenerator.Domain;

public class GenerationRequest
{
    public string SolutionName { get; set; } 
    public string ProjectName { get; set; } 
    public string DatabaseName{ get; set; } 
    public string DatabaseServer { get; set; }
    public string DatabasePort { get; set; }
    public string DatabaseUid { get; set; }
    public string DatabasePwd { get; set; }
    public string Provider { get; set; }
    public string Architecture { get; set; }
}