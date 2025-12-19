using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.DBInteractions;

public class ConnectionManager
{
    private readonly string connectionString;

    public ConnectionManager(string server, string databaseName, string userName, string password)
    {
        MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder
        {
            Server = server,
            Database = databaseName,
            UserID = userName,
            Password = password
        };
        connectionString = builder.ConnectionString;
    }

    public MySqlConnection IssueConnection()
    {
        MySqlConnection connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }
}