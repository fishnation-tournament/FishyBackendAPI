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
            user.DiscordUsername = reader.IsDBNull(7) ? null : reader.GetString(7);
            user.GoogleID = reader.IsDBNull(8) ? null : reader.GetString(8);
            user.GoogleEmail = reader.IsDBNull(9) ? null : reader.GetString(9);
            user.RegistrationDate = reader.GetDateTime(10);
            user.Role = reader.IsDBNull(11) ? null : reader.GetString(11);
            user.FrontendRole = reader.IsDBNull(12) ? null : reader.GetString(12);

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
            user.DiscordUsername = reader.IsDBNull(7) ? null : reader.GetString(7);
            user.GoogleID = reader.IsDBNull(8) ? null : reader.GetString(8);
            user.GoogleEmail = reader.IsDBNull(9) ? null : reader.GetString(9);
            user.RegistrationDate = reader.GetDateTime(10);
            user.Role = reader.IsDBNull(11) ? null : reader.GetString(11);
            user.FrontendRole = reader.IsDBNull(12) ? null : reader.GetString(12);
        }
        reader.Close();

        return user;
    }

    public static List<DataTypes.User> GetUsers(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.User> users = ReadResults(reader);

        return users;
    }

    public static DataTypes.User GetUserById(MySqlConnection dbConn, ulong UID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE UserID = @UID", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.User user = ReadSingleResult(reader);

        return user;
    }

    public static DataTypes.User GetUserBySSID(MySqlConnection dbConn, ulong SSID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE SSID = @SSID", dbConn);
        cmd.Parameters.AddWithValue("@SSID", SSID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.User user = ReadSingleResult(reader);

        return user;
    }

    public static List<DataTypes.User> GetUsersByOptBLUID(MySqlConnection dbConn, ulong OptBLUID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE OptBLUID = @OptBLUID", dbConn);
        cmd.Parameters.AddWithValue("@OptBLUID", OptBLUID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.User> users = ReadResults(reader);

        return users;
    }

    public static List<DataTypes.User> GetUsersByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Username LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.User> users = ReadResults(reader);

        return users;
    }

    public static List<DataTypes.User> GetUsersByRole(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Role LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.User> users = ReadResults(reader);

        return users;
    }

    public static DataTypes.User GetUserByDiscordId(MySqlConnection dbConn, ulong DiscordID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE DiscordID = @DiscordID", dbConn);
        cmd.Parameters.AddWithValue("@DiscordID", DiscordID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.User user = ReadSingleResult(reader);

        return user;
    }

    public static DataTypes.User AddUser(MySqlConnection dbConn, FrontFacingDataTypes.AddUser user)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO users
            (SSID, OptBLUID, Username, UserPfpLink, Description, DiscordID, DiscordName, GoogleID, GoogleEmail, RegistrationDate, Role, FrontendRole)
        VALUES
            (@SSID, @OptBLUID, @Username, @UserPfpLink, @Description, @DiscordID, @DiscordName, @GoogleID, @GoogleEmail, @RegistrationDate, @Role, @FrontendRole)
        """, dbConn);
        cmd.Parameters.AddWithValue("@SSID", user.SSID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OptBLUID", user.OptBLUID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@UserPfpLink", user.UserPfpLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", user.UserBio ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordID", user.DiscordID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordName", user.DiscordUsername ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleID", user.GoogleID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleEmail", user.GoogleEmail ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Role", user.Role ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@FrontendRole", user.FrontendRole ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        MySqlCommand getIdQuery = new MySqlCommand("SELECT * FROM users WHERE UserID = @UserID", dbConn);
        getIdQuery.Parameters.AddWithValue("@UserID", cmd.LastInsertedId);
        using MySqlDataReader reader = getIdQuery.ExecuteReader();
        DataTypes.User newUser = ReadSingleResult(reader);

        return newUser;
    }

    public static void UpdateUser(MySqlConnection dbConn, DataTypes.User user)
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE users SET
            SSID = @SSID,
            OptBLUID = @OptBLUID,
            Username = @Username,
            UserPfpLink = @UserPfpLink,
            Description = @Description,
            DiscordID = @DiscordID,
            DiscordName = @DiscordName,
            GoogleID = @GoogleID,
            GoogleEmail = @GoogleEmail,
            RegistrationDate = @RegistrationDate,
            Role = @Role,
            FrontendRole = @FrontendRole
        WHERE UserID = @UserID
        """, dbConn);
        cmd.Parameters.AddWithValue("@SSID", user.SSID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OptBLUID", user.OptBLUID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@UserPfpLink", user.UserPfpLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", user.UserBio ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordID", user.DiscordID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordName", user.DiscordUsername ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleID", user.GoogleID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleEmail", user.GoogleEmail ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Role", user.Role ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@FrontendRole", user.FrontendRole ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@UserID", user.UID);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteUser(MySqlConnection dbConn, ulong UID)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM users WHERE UserID = @UID", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        cmd.ExecuteNonQuery();
    }

    public static bool CheckUserExistsDID(MySqlConnection dbConn, ulong DiscordID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE DiscordID = @DiscordID", dbConn);
        cmd.Parameters.AddWithValue("@DiscordID", DiscordID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return reader.HasRows;
    }

    public static DataTypes.User GetUserByGoogleId(MySqlConnection dbConn, string GoogleID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE GoogleID = @GoogleID", dbConn);
        cmd.Parameters.AddWithValue("@GoogleID", GoogleID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.User user = ReadSingleResult(reader);

        return user;
    }

    public static bool CheckUserExistsGID(MySqlConnection dbConn, string GoogleID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE GoogleID = @GoogleID", dbConn);
        cmd.Parameters.AddWithValue("@GoogleID", GoogleID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return reader.HasRows;
    }

    public static bool CheckUserExistsSteamID(MySqlConnection dbConn, ulong SteamID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE SSID = @SteamID OR OptBLUID = @SteamID", dbConn);
        cmd.Parameters.AddWithValue("@SteamID", SteamID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return reader.HasRows;
    }
}
