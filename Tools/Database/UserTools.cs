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
            user.OptBLUID = reader.GetUInt64(1);
            user.Username = reader.GetString(2);
            user.UserPfpLink = reader.GetString(3);
            user.UserBio = reader.GetString(4);
            user.DiscordID = reader.GetUInt64(5);
            user.DiscordUsername = reader.GetString(6);
            user.RegistrationDate = reader.GetDateTime(7);
            user.Role = reader.GetString(8);
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
            user.OptBLUID = reader.GetUInt64(1);
            user.Username = reader.GetString(2);
            user.UserPfpLink = reader.GetString(3);
            user.UserBio = reader.GetString(4);
            user.DiscordID = reader.GetUInt64(5);
            user.DiscordUsername = reader.GetString(6);
            user.RegistrationDate = reader.GetDateTime(7);
            user.Role = reader.GetString(8);
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
        string query = "INSERT INTO users (UID, OptBLUID, Username, UserPfpLink, Description, DiscordID, DiscordName, RegistrationDate, Role) VALUES (" + user.UID + ", " + user.OptBLUID + ", '" + user.Username + "', '" + user.UserPfpLink + "', '" + user.UserBio + "', " + user.DiscordID + ", '" + user.DiscordUsername + "', '" + user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + user.Role + "')";
        interactionHelper.SendCommand(query);
    }
    
    public static void UpdateUser(SQLInteraction interactionHelper, DataTypes.User user)
    {
        string query = "UPDATE users SET OptBLUID = " + user.OptBLUID + ", Username = '" + user.Username + "', UserPfpLink = '" + user.UserPfpLink + "', Description = '" + user.UserBio + "', DiscordID = " + user.DiscordID + ", DiscordName = '" + user.DiscordUsername + "', RegistrationDate = '" + user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss") + "', Role = '" + user.Role + "' WHERE UID = " + user.UID;
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