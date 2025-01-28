using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.DBInteractions;

public class ConnectionManager
{
    public ConnectionManager(string server, string databaseName, string userName, string password)
    {
        Server = server;
        DatabaseName = databaseName;
        UserName = userName;
        Password = password;
    }

    private static string Server;
    private static string DatabaseName;
    private static string UserName;
    private static string Password;

    public DBConnection IssueConnection()
    {
        var dbConn = DBConnection.Instance();
        dbConn.Server = Server;
        dbConn.DatabaseName = DatabaseName;
        dbConn.UserName = UserName;
        dbConn.Password = Password;
        
        if (dbConn.IsConnect())
        {
            return dbConn;
        }

        return null;
    }
}