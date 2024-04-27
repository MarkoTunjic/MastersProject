namespace GrpcGenerator.Application.Services;

public interface IGeneratorService
{
    byte[] GetZipProject(string solutionName, string projectName, string databaseName,
        string databaseServer, string databasePort, string databaseUid, string databasePwd,
        string provider, string architecture);
}