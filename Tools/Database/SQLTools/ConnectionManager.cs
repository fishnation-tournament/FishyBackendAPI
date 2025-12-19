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

    public MySqlConnection IssueConnection()
    {
        MySqlConnection connection = new MySqlConnection(string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password));
        connection.Open();
        return connection;
    }
}