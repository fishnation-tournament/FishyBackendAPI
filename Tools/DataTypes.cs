using System.Web;
using System.Collections.Specialized;
using System.Text.Json;
using FishyAPI.Tools.FrontFacingDataTypes;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.DataTypes;

public struct User
{
    public ulong UID { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(NullableUInt64ToStringConverter))]
    public ulong? SSID { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(NullableUInt64ToStringConverter))]
    public ulong? OptBLUID { get; set; }

    public string Username { get; set; }
    public string? UserPfpLink { get; set; }
    public string? UserBio { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(NullableUInt64ToStringConverter))]
    public ulong? DiscordID { get; set; }
    public string? DiscordUsername { get; set; }

    public string? GoogleID { get; set; }
    public string? GoogleEmail { get; set; }

    public DateTime RegistrationDate { get; set; }
    public string? Role { get; set; }
    public string? FrontendRole { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(RawJsonStringConverter))]
    public string? ScoreSaberProfile { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(RawJsonStringConverter))]
    public string? BeatLeaderProfile { get; set; }

    public User(MySqlDataReader reader)
    {
        UID = reader.GetUInt64(0);
        SSID = reader.IsDBNull(1) ? null : reader.GetUInt64(1);
        OptBLUID = reader.IsDBNull(2) ? null : reader.GetUInt64(2);
        Username = reader.GetString(3);
        UserPfpLink = reader.IsDBNull(4) ? null : reader.GetString(4);
        UserBio = reader.IsDBNull(5) ? null : reader.GetString(5);
        DiscordID = reader.IsDBNull(6) ? null : reader.GetUInt64(6);
        DiscordUsername = reader.IsDBNull(7) ? null : reader.GetString(7);
        GoogleID = reader.IsDBNull(8) ? null : reader.GetString(8);
        GoogleEmail = reader.IsDBNull(9) ? null : reader.GetString(9);
        RegistrationDate = reader.GetDateTime(10);
        Role = reader.IsDBNull(11) ? null : reader.GetString(11);
        FrontendRole = reader.IsDBNull(12) ? null : reader.GetString(12);
        string? ssProf = reader.IsDBNull(13) ? null : reader.GetString(13);
        string? blProf = reader.IsDBNull(14) ? null : reader.GetString(14);

        ScoreSaberProfile = NormalizeStoredProfileJson(ssProf, UID, "ScoreSaberProfile");
        BeatLeaderProfile = NormalizeStoredProfileJson(blProf, UID, "BeatLeaderProfile");
    }

    private static string? NormalizeStoredProfileJson(string? profileJson, ulong uid, string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileJson))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(profileJson);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                string? innerJson = doc.RootElement.GetString();
                if (string.IsNullOrWhiteSpace(innerJson))
                {
                    return null;
                }

                using JsonDocument innerDoc = JsonDocument.Parse(innerJson);
                return innerDoc.RootElement.GetRawText();
            }

            return doc.RootElement.GetRawText();
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Failed to deserialize {profileName} for UID {uid}: {ex.Message}");
            return null;
        }
    }

    public async Task<User> UpdateProfilesAsync()
    {
        Console.WriteLine($"Updating profiles for user {Username} (SSID: {SSID}, OptBLUID: {OptBLUID})");
        UserProfiles userProfiles = await UserProfiles.GetUserProfilesAsync(SSID, OptBLUID);
        ScoreSaberProfile = userProfiles.ScoreSaberProfile;
        BeatLeaderProfile = userProfiles.BeatLeaderProfile;

        return this;
    }
}

public struct UserProfiles
{
    public string? ScoreSaberProfile { get; set; }
    public string? BeatLeaderProfile { get; set; }

    // No async constructors :(
    public static async Task<UserProfiles> GetUserProfilesAsync(ulong? SSID, ulong? OptBLUID)
    {
        using var httpClient = new HttpClient();

        UserProfiles userProfiles = new UserProfiles { ScoreSaberProfile = null, BeatLeaderProfile = null };

        if (SSID != null)
        {
            var scoreSaberResponse = await httpClient.GetAsync($"https://scoresaber.com/api/player/{SSID}/full");
            if (scoreSaberResponse.IsSuccessStatusCode)
            {
                userProfiles.ScoreSaberProfile = await scoreSaberResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"ScoreSaberProfile: {userProfiles.ScoreSaberProfile}");
            }
        }

        if (OptBLUID != null)
        {
            NameValueCollection beatLeaderParameters = HttpUtility.ParseQueryString(string.Empty);
            beatLeaderParameters["stats"] = "true";
            beatLeaderParameters["keepOriginalId"] = "false";
            beatLeaderParameters["leaderboardContext"] = "general";

            var beatLeaderResponse = await httpClient.GetAsync($"https://api.beatleader.com/player/{OptBLUID}?{beatLeaderParameters}");
            if (beatLeaderResponse.IsSuccessStatusCode)
            {
                userProfiles.BeatLeaderProfile = await beatLeaderResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"BeatLeaderProfile: {userProfiles.BeatLeaderProfile}");
            }
        }

        return userProfiles;
    }
}

public struct Player
{
    public int? PlayerID { get; set; }
    public ulong UID { get; set; }
    public int Season { get; set; }
    public int? Seed { get; set; }
    public FrontFacingUser User { get; set; }
    
    public Player(MySqlDataReader reader)
    {
        PlayerID = reader.GetInt32(0);
        UID = reader.GetUInt64(1);
        Season = reader.GetInt32(2);
        Seed =  reader.IsDBNull(3) ? null : reader.GetInt32(3);
        User = new FrontFacingUser();
    }
}


public struct Map
{
    public ulong MID { get; set; }
    public string BSMapID { get; set; }
    public string MapName { get; set; }
    public string MapImageLink { get; set; }
    public ulong MapPoolID { get; set; }
    public int Season { get; set; }
    public string Difficulty { get; set; }
    public List<string>? Tags { get; set; }

    private static int GetOrdinalOrMinusOne(MySqlDataReader reader, string columnName)
    {
        try
        {
            return reader.GetOrdinal(columnName);
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }

    public Map(MySqlDataReader reader)
    {
        MID = reader.GetUInt64(reader.GetOrdinal("MapID"));
        BSMapID = reader.GetString(reader.GetOrdinal("BSMapID"));
        MapName = reader.GetString(reader.GetOrdinal("MapName"));
        MapImageLink = reader.GetString(reader.GetOrdinal("MapImageLink"));
        MapPoolID = reader.GetUInt64(reader.GetOrdinal("MPID"));
        Season = reader.GetInt32(reader.GetOrdinal("Season"));
        Difficulty = reader.GetString(reader.GetOrdinal("Difficulty"));

        int tagsOrdinal = GetOrdinalOrMinusOne(reader, "Tags");
        Tags = tagsOrdinal < 0 || reader.IsDBNull(tagsOrdinal)
            ? null
            : reader.GetString(tagsOrdinal).Split(',').ToList();
    }
}

public struct MapPool
{
    public ulong MapPoolID { get; set; }
    public string MapPoolName { get; set; }
    public string MapPoolDescription { get; set; }
    public int Season { get; set; }
    public string? DownloadLink { get; set; }
    public string? OneClickLink { get; set; }
    public string Visibility { get; set; }
    public List<Map> Maps { get; set; }

    public MapPool(MySqlDataReader reader)
    {
        MapPoolID = reader.GetUInt64(0);
        MapPoolName = reader.GetString(1);
        MapPoolDescription = reader.GetString(2);
        Season = reader.GetInt32(3);
        DownloadLink = reader.IsDBNull(4) ? null : reader.GetString(4);
        OneClickLink = reader.IsDBNull(5) ? null : reader.GetString(5);
        Visibility = reader.GetString(6);
        Maps = new List<Map>();
    }
}

public struct Match
{
    public ulong MatchID { get; set; }
    public ulong Player1ID { get; set; }
    public ulong Player2ID { get; set; }
    public bool Complete { get; set; }
    public ulong? WinnerID { get; set; }
    public int? Player1Score { get; set; }
    public int? Player2Score { get; set; }
    public DateTime? MatchDate { get; set; }
    public List<MatchScore> Scores { get; set; }
    public int Season { get; set; }
    public ulong MapPoolID { get; set; }

    public Match(MySqlDataReader reader)
    {
        MatchID = reader.GetUInt64(0);
        Player1ID = reader.GetUInt64(1);
        Player2ID = reader.GetUInt64(2);
        Player1Score = reader.IsDBNull(3) ? null : reader.GetInt32(3);
        Player2Score = reader.IsDBNull(4) ? null : reader.GetInt32(4);
        MatchDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
        Complete = reader.GetBoolean(6);
        WinnerID = reader.IsDBNull(7) ? null : reader.GetUInt64(7);
        Season = reader.GetInt32(8);
        MapPoolID = reader.GetUInt64(9);
        Scores = new List<MatchScore>();
    }
}

public struct MatchScore
{
    public ulong ScoreID { get; set; }
    public ulong UID { get; set; }
    public ulong MapID { get; set; }
    public ulong MatchID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }

    public MatchScore(MySqlDataReader reader)
    {
        ScoreID = reader.GetUInt64(0);
        UID = reader.GetUInt64(1);
        MapID = reader.GetUInt64(2);
        MatchID = reader.GetUInt64(3);
        Score = reader.GetInt32(4);
        MaxScore = reader.GetInt32(5);
        Misses = reader.GetInt32(6);
    }
}

public struct QualifierScore
{
    public ulong ScoreID { get; set; }
    public ulong UID { get; set; }
    public ulong MapID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public ulong QualifierID { get; set; }

    public QualifierScore(MySqlDataReader reader)
    {
        ScoreID = reader.GetUInt64(0);
        UID = reader.GetUInt64(1);
        MapID = reader.GetUInt64(2);
        Score = reader.GetInt32(3);
        MaxScore = reader.GetInt32(4);
        Misses = reader.GetInt32(5);
        QualifierID = reader.GetUInt64(6);
    }
}

public struct ApiToken
{
    public string TokenID { get; set; }
    public ulong OwnerUID { get; set; }
    public string TokenName { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    public ApiToken(MySqlDataReader reader)
    {
        TokenID = reader.GetString(0);
        OwnerUID = reader.GetUInt64(1);
        TokenName = reader.GetString(2);
        Role = reader.GetString(3);
        CreatedAt = reader.GetDateTime(4);
        ExpiresAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
        IsRevoked = reader.GetBoolean(6);
    }
}
