using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace FishyAPI.Tools;

public static class UserTools
{
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
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        return users;
    }

    public static User GetUserById(MySqlConnection dbConn, ulong UID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE UserID = @UID", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);

        return user;
    }

    public static User GetUserBySSID(MySqlConnection dbConn, ulong SSID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE SSID = @SSID", dbConn);
        cmd.Parameters.AddWithValue("@SSID", SSID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);

        return user;
    }

    public static List<User> GetUsersByOptBLUID(MySqlConnection dbConn, ulong OptBLUID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE OptBLUID = @OptBLUID", dbConn);
        cmd.Parameters.AddWithValue("@OptBLUID", OptBLUID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        return users;
    }

    public static List<User> GetUsersByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Username LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        return users;
    }

    public static List<User> GetUsersByRole(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Role LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<User> users = ReadResults(reader);

        return users;
    }

    public static User GetUserByDiscordId(MySqlConnection dbConn, ulong DiscordID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE DiscordID = @DiscordID", dbConn);
        cmd.Parameters.AddWithValue("@DiscordID", DiscordID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);

        return user;
    }

    public static User AddUser(MySqlConnection dbConn, AddUser user)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO users
            (SSID, OptBLUID, Username, UserPfpLink, Description, DiscordID, DiscordName, GoogleID, GoogleEmail, RegistrationDate, Role, FrontendRole, ScoreSaberProfile, BeatLeaderProfile)
        VALUES
            (@SSID, @OptBLUID, @Username, @UserPfpLink, @Description, @DiscordID, @DiscordName, @GoogleID, @GoogleEmail, @RegistrationDate, @Role, @FrontendRole, @ScoreSaberProfile, @BeatLeaderProfile)
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
        cmd.Parameters.AddWithValue("@ScoreSaberProfile", user.ScoreSaberProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BeatLeaderProfile", user.BeatLeaderProfile ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        return GetUserById(dbConn, (ulong)cmd.LastInsertedId);
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
            GoogleID = @GoogleID,
            GoogleEmail = @GoogleEmail,
            RegistrationDate = @RegistrationDate,
            Role = @Role,
            FrontendRole = @FrontendRole,
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
        cmd.Parameters.AddWithValue("@GoogleID", user.GoogleID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@GoogleEmail", user.GoogleEmail ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Role", user.Role ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@FrontendRole", user.FrontendRole ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ScoreSaberProfile", user.ScoreSaberProfile ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BeatLeaderProfile", user.BeatLeaderProfile ?? (object)DBNull.Value);
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

    public static User GetUserByGoogleId(MySqlConnection dbConn, string GoogleID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE GoogleID = @GoogleID", dbConn);
        cmd.Parameters.AddWithValue("@GoogleID", GoogleID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        User user = ReadSingleResult(reader);

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
