namespace GrpcGenerator.Domain;

public class DatabaseConnectionData
{
    public DatabaseConnectionData(string databaseServer, string databaseName, string databasePort, string databasePwd,
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

    public string ToConnectionString()
    {
        if (Provider == "sqlserver")
        {
            return $"Server={DatabaseServer};Database={DatabaseName};Uid={DatabaseUid};Pwd={DatabasePwd};";
        }
        return
            $"Server={DatabaseServer};Port={DatabasePort};Database={DatabaseName};Uid={DatabaseUid};Pwd={DatabasePwd};";
    }
}