using MySql.Data.MySqlClient;

namespace FishyAPI.Tools;

public static class UserTools
{
    private static List<DataTypes.User> ReadResults(MySqlDataReader reader)
    {
        List<DataTypes.User> users = new List<DataTypes.User>();
        while (reader.Read())
        {
            DataTypes.User user = new DataTypes.User();
            user.UID = reader.GetUInt64(0);
            user.SSID = reader.IsDBNull(1) ? null : reader.GetUInt64(1);
            user.OptBLUID = reader.IsDBNull(2) ? null : reader.GetUInt64(2);
            user.Username = reader.GetString(3);
            user.UserPfpLink = reader.IsDBNull(4) ? null : reader.GetString(4);
            user.UserBio = reader.IsDBNull(5) ? null : reader.GetString(5);
            user.DiscordID = reader.IsDBNull(6) ? null : reader.GetUInt64(6);
            user.DiscordUsername = reader.GetString(7);
            user.RegistrationDate = reader.GetDateTime(8);
            user.Role = reader.IsDBNull(9) ? null : reader.GetString(9);
            user.FrontendRole = reader.IsDBNull(10) ? null : reader.GetString(10);
            users.Add(user);
        }
        reader.Close();
        
        return users;
    }
    
    private static DataTypes.User ReadSingleResult(MySqlDataReader reader)
    {
        DataTypes.User user = new DataTypes.User();
        while (reader.Read())
        {
            user.UID = reader.GetUInt64(0);
            user.SSID = reader.IsDBNull(1) ? null : reader.GetUInt64(1);
            user.OptBLUID = reader.IsDBNull(2) ? null : reader.GetUInt64(2);
            user.Username = reader.GetString(3);
            user.UserPfpLink = reader.IsDBNull(4) ? null : reader.GetString(4);
            user.UserBio = reader.IsDBNull(5) ? null : reader.GetString(5);
            user.DiscordID = reader.IsDBNull(6) ? null : reader.GetUInt64(6);
            user.DiscordUsername = reader.GetString(7);
            user.RegistrationDate = reader.GetDateTime(8);
            user.Role = reader.IsDBNull(9) ? null : reader.GetString(9);
            user.FrontendRole = reader.IsDBNull(10) ? null : reader.GetString(10);
        }
        reader.Close();
        
        return user;
    }
    public static List<DataTypes.User> GetUsers(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM users";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.User> users = ReadResults(reader);
        
        return users;
    }
    
    public static DataTypes.User GetUserById(SQLInteraction interactionHelper, ulong UID)
    {
        string query = $"SELECT * FROM users WHERE UID = {UID}";
        using (var reader = interactionHelper.GetReader(query))
        {
            var user = ReadSingleResult(reader);
            return user;
        }
    }
    
    public static DataTypes.User GetUserBySSID(SQLInteraction interactionHelper, ulong SSID)
    {
        string query = $"SELECT * FROM users WHERE SSID = {SSID}";
        using (var reader = interactionHelper.GetReader(query))
        {
            var user = ReadSingleResult(reader);
            return user;
        }
    }
    
    public static List<DataTypes.User> GetUsersByOptBLUID(SQLInteraction interactionHelper, ulong OptBLUID)
    {
        string query = $"SELECT * FROM users WHERE OptBLUID = {OptBLUID}";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.User> users = ReadResults(reader);
        
        return users;
    }
    
    public static List<DataTypes.User> GetUsersByName(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM users WHERE Username LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.User> users = ReadResults(reader);
        
        return users;
    }
    
    public static List<DataTypes.User> GetUsersByRole(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM users WHERE Role LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.User> users = ReadResults(reader);
        
        return users;
    }
    
    public static DataTypes.User GetUserByDiscordId(SQLInteraction interactionHelper, ulong DiscordID)
    {
        string query = "SELECT * FROM users WHERE DiscordID = " + DiscordID;
        var reader = interactionHelper.GetReader(query);
        DataTypes.User userRes = ReadSingleResult(reader);
        
        return userRes;
    }
    
    public static void AddUser(SQLInteraction interactionHelper, DataTypes.User user)
    {
        string query = "INSERT INTO users (SSID, OptBLUID, Username, UserPfpLink, Description, DiscordID, DiscordName, RegistrationDate, Role, FrontendRole) VALUES (" + user.SSID + ", " + user.OptBLUID + ", '" + user.Username + "', '" + user.UserPfpLink + "', '" + user.UserBio + "', " + user.DiscordID + ", '" + user.DiscordUsername + "', '" + user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + user.Role + "', '" + user.FrontendRole + "')";
        interactionHelper.SendCommand(query);
    }
    
    public static void UpdateUser(SQLInteraction interactionHelper, DataTypes.User user)
    {
        string query = "UPDATE users SET SSID = " + user.SSID + " OptBLUID = " + user.OptBLUID + ", Username = '" + user.Username + "', UserPfpLink = '" + user.UserPfpLink + "', Description = '" + user.UserBio + "', DiscordID = " + user.DiscordID + ", DiscordName = '" + user.DiscordUsername + "', RegistrationDate = '" + user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss") + "', Role = '" + user.Role + "', FrontendRole = '" + user.FrontendRole + "' WHERE UID = " + user.UID;
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteUser(SQLInteraction interactionHelper, ulong UID)
    {
        string query = $"DELETE FROM users WHERE UID = {UID}";
        interactionHelper.SendCommand(query);
    }
    
    public static bool CheckUserExistsDID(SQLInteraction interactionHelper, ulong DiscordID)
    {
        string query = $"SELECT * FROM users WHERE DiscordID = {DiscordID}";
        var reader = interactionHelper.GetReader(query);
        if (reader.HasRows)
        {
            reader.Close();
            return true;
        }
        reader.Close();
        return false;
    }
}