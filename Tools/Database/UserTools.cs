using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace FishyAPI.Tools;

public static class UserTools
{
    /// <summary>Column list read by User(reader); RoleKeys is the user's roles as a comma-separated list.</summary>
    private const string UserColumns = """
        u.UserID, u.SSID, u.OptBLUID, u.Username, u.UserPfpLink, u.Description, u.DiscordID, u.DiscordName,
        u.RegistrationDate, u.Title, u.ScoreSaberProfile, u.BeatLeaderProfile,
        (SELECT GROUP_CONCAT(r.RoleKey ORDER BY r.RoleID) FROM user_roles ur JOIN roles r ON r.RoleID = ur.RoleID
         WHERE ur.UserID = u.UserID) AS RoleKeys
        """;

    private static List<User> ReadResults(MySqlDataReader reader)
    {
        List<User> users = new List<User>();
        while (reader.Read())
        {
            users.Add(new User(reader));
        }

        return users;
    }

    private static User ReadSingleResult(MySqlDataReader reader)
    {
        User user = new User();
        if (reader.Read())
        {
            user = new User(reader);
        }

        return user;
    }

    public static List<User> GetUsers(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {UserColumns} FROM users u", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        reader.Close();
        return users;
    }

    public static User GetUserById(MySqlConnection dbConn, ulong UID)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {UserColumns} FROM users u WHERE u.UserID = @UID", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);
        
        reader.Close();
        return user;
    }

    public static User GetUserBySSID(MySqlConnection dbConn, ulong SSID)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {UserColumns} FROM users u WHERE u.SSID = @SSID", dbConn);
        cmd.Parameters.AddWithValue("@SSID", SSID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);

        reader.Close();
        return user;
    }

    public static List<User> GetUsersByOptBLUID(MySqlConnection dbConn, ulong OptBLUID)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {UserColumns} FROM users u WHERE u.OptBLUID = @OptBLUID", dbConn);
        cmd.Parameters.AddWithValue("@OptBLUID", OptBLUID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        reader.Close();
        return users;
    }

    public static List<User> GetUsersByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {UserColumns} FROM users u WHERE u.Username LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        reader.Close();
        return users;
    }

    public static List<User> GetUsersByRole(MySqlConnection dbConn, string searchTerm)
    {
        // Matches a role's key ('map_pooler') or display name ('Map Pooler').
        MySqlCommand cmd = new MySqlCommand($"""
            SELECT {UserColumns} FROM users u
            WHERE EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.RoleID = ur.RoleID
                          WHERE ur.UserID = u.UserID AND (r.RoleKey = @role OR r.DisplayName = @role))
            """, dbConn);
        cmd.Parameters.AddWithValue("@role", searchTerm);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        reader.Close();
        return users;
    }

    public static User GetUserByDiscordId(MySqlConnection dbConn, ulong DiscordID)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {UserColumns} FROM users u WHERE u.DiscordID = @DiscordID", dbConn);
        cmd.Parameters.AddWithValue("@DiscordID", DiscordID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);

        reader.Close();
        return user;
    }

    public static User AddUser(MySqlConnection dbConn, AddUser user)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO users
            (SSID, OptBLUID, Username, UserPfpLink, Description, DiscordID, DiscordName, RegistrationDate, Title, ScoreSaberProfile, BeatLeaderProfile)
        VALUES
            (@SSID, @OptBLUID, @Username, @UserPfpLink, @Description, @DiscordID, @DiscordName, @RegistrationDate, @Title, @ScoreSaberProfile, @BeatLeaderProfile)
        """, dbConn);
        cmd.Parameters.AddWithValue("@SSID", user.SSID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OptBLUID", user.OptBLUID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@UserPfpLink", user.UserPfpLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", user.UserBio ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordID", user.DiscordID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordName", user.DiscordUsername ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Title", user.Title ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ScoreSaberProfile", user.ScoreSaberProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BeatLeaderProfile", user.BeatLeaderProfile ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        // Every account starts as a member; anything more is assigned through the role endpoints.
        ulong newUid = (ulong)cmd.LastInsertedId;
        RoleTools.AssignRole(dbConn, newUid, "user", null);
        return GetUserById(dbConn, newUid);
    }

    public static void UpdateUser(MySqlConnection dbConn, User user)
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
            RegistrationDate = @RegistrationDate,
            Title = @Title,
            ScoreSaberProfile = @ScoreSaberProfile,
            BeatLeaderProfile = @BeatLeaderProfile
        WHERE UserID = @UserID
        """, dbConn);
        cmd.Parameters.AddWithValue("@SSID", user.SSID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@OptBLUID", user.OptBLUID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@UserPfpLink", user.UserPfpLink ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Description", user.UserBio ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordID", user.DiscordID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DiscordName", user.DiscordUsername ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Title", user.Title ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ScoreSaberProfile", user.ScoreSaberProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BeatLeaderProfile", user.BeatLeaderProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@UserID", user.UID);
        cmd.ExecuteNonQuery();

        // Keep the players table in sync with the user's ScoreSaber ID.
        MySqlCommand syncCmd = new MySqlCommand(
            "UPDATE players SET ScoresaberID = @SSID WHERE UID = @UID",
            dbConn);
        syncCmd.Parameters.AddWithValue("@SSID", user.SSID ?? (object)DBNull.Value);
        syncCmd.Parameters.AddWithValue("@UID", user.UID);
        syncCmd.ExecuteNonQuery();

        Console.WriteLine($"Updated user {user.Username} (UID: {user.UID}) in the database. {user.Username} now has Title: {user.Title}, SSID: {user.SSID}, OptBLUID: {user.OptBLUID}");
    }

    /// <summary>Every user registered as a player in any season who has a ScoreSaber or BeatLeader ID linked.</summary>
    public static List<(ulong UID, ulong? SSID, ulong? OptBLUID)> GetPlayerProfileIds(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("""
        SELECT u.UserID, u.SSID, u.OptBLUID
        FROM users u
        WHERE (u.SSID IS NOT NULL OR u.OptBLUID IS NOT NULL)
          AND EXISTS (SELECT 1 FROM players p WHERE p.UID = u.UserID)
        ORDER BY u.UserID
        """, dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();

        List<(ulong, ulong?, ulong?)> ids = new List<(ulong, ulong?, ulong?)>();
        while (reader.Read())
        {
            ids.Add((
                reader.GetUInt64(0),
                reader.IsDBNull(1) ? null : reader.GetUInt64(1),
                reader.IsDBNull(2) ? null : reader.GetUInt64(2)));
        }

        reader.Close();
        return ids;
    }

    /// <summary>Writes only the profile columns; a null profile leaves the stored one untouched.</summary>
    public static void UpdateUserProfiles(MySqlConnection dbConn, ulong UID, string? scoreSaberProfile, string? beatLeaderProfile)
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE users SET
            ScoreSaberProfile = COALESCE(@ScoreSaberProfile, ScoreSaberProfile),
            BeatLeaderProfile = COALESCE(@BeatLeaderProfile, BeatLeaderProfile)
        WHERE UserID = @UserID
        """, dbConn);
        cmd.Parameters.AddWithValue("@ScoreSaberProfile", scoreSaberProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BeatLeaderProfile", beatLeaderProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@UserID", UID);
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
        MySqlCommand cmd = new MySqlCommand("SELECT 1 FROM users WHERE DiscordID = @DiscordID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@DiscordID", DiscordID);
        object? result = cmd.ExecuteScalar();
        return result != null;
    }

    public static bool CheckUserExistsBySSID(MySqlConnection dbConn, ulong ssid)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT 1 FROM users WHERE SSID = @SSID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@SSID", ssid);
        object? result = cmd.ExecuteScalar();
        return result != null;
    }

    public static bool CheckUserExistsSteamID(MySqlConnection dbConn, ulong steamId)
        => CheckUserExistsBySSID(dbConn, steamId);

    public static ulong? GetBLID(string discordID)
    {
        if (string.IsNullOrWhiteSpace(discordID)) return null;

        using HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri($"https://api.beatkhana.com/api/users/{discordID}");

        using var response = httpClient.GetAsync(string.Empty).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) return null;

        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(content)) return null;

        BeatKhanaUserResponse? beatKhanaUser;
        try
        {
            beatKhanaUser = JsonConvert.DeserializeObject<BeatKhanaUserResponse>(content);
        }
        catch (JsonException)
        {
            return null;
        }

        if (!ulong.TryParse(beatKhanaUser?.BeatLeaderId, out ulong beatLeaderId)) return null;
        return beatLeaderId;
    }

    private sealed class BeatKhanaUserResponse
    {
        [JsonProperty("beatleaderId")]
        public string? BeatLeaderId { get; set; }
    }
}
