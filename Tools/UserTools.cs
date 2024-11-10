using FishyAPI.Tools.DBInteractions;

namespace FishyAPI.Tools;

public static class UserTools
{
    public static List<DataTypes.User> GetUsers(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM users";
        var reader = interactionHelper.GetReader(query);
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
        return users;
    }
    
    public static DataTypes.User GetUserById(SQLInteraction interactionHelper, ulong UID)
    {
        string query = "SELECT * FROM users WHERE UID = " + UID + " LIMIT 1";
        var reader = interactionHelper.GetReader(query);
        DataTypes.User userRes = new DataTypes.User();
        userRes.UID = reader.GetUInt64(0);
        userRes.OptBLUID = reader.GetUInt64(1);
        userRes.Username = reader.GetString(2);
        userRes.UserPfpLink = reader.GetString(3);
        userRes.UserBio = reader.GetString(4);
        userRes.DiscordID = reader.GetUInt64(5);
        userRes.DiscordUsername = reader.GetString(6);
        userRes.RegistrationDate = reader.GetDateTime(7);
        userRes.Role = reader.GetString(8);
        return userRes;
    }
    
    public static List<DataTypes.User> SearchUserByName(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM users WHERE Username LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
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
        return users;
    }
    
    public static void UpdateUser(SQLInteraction interactionHelper, DataTypes.User user)
    {
        string query = "UPDATE users SET OptBLUID = " + user.OptBLUID + ", Username = '" + user.Username + "', UserPfpLink = '" + user.UserPfpLink + "', UserBio = '" + user.UserBio + "', DiscordID = " + user.DiscordID + ", DiscordUsername = '" + user.DiscordUsername + "', RegistrationDate = '" + user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss") + "', Role = '" + user.Role + "' WHERE UID = " + user.UID;
        interactionHelper.SendCommand(query);
    }
}