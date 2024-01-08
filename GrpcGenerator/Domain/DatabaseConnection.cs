namespace GrpcGenerator.Domain;

public class DatabaseConnection
{
    public DatabaseConnection(string databaseServer, string databaseName, string databasePort, string databasePwd,
        string databaseUid, string provider)
    {
        DatabaseServer = databaseServer;
        DatabaseName = databaseName;
        DatabasePort = databasePort;
        DatabasePwd = databasePwd;
        DatabaseUid = databaseUid;
        Provider = provider;
    }

    public string DatabaseServer { get; set; }
    public string DatabaseName { get; set; }
    public string DatabasePort { get; set; }
    public string DatabasePwd { get; set; }
    public string DatabaseUid { get; set; }
    public string Provider { get; set; }
}