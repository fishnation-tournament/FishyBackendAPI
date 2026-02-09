using System.Text.Json.Serialization;
using System.Web;
using System.Collections.Specialized;
using MySql.Data.MySqlClient;
namespace FishyAPI.Tools.DataTypes;

public struct User
{
    public ulong UID { get; set; }

    [JsonConverter(typeof(NullableUInt64ToStringConverter))]
    public ulong? SSID { get; set; }

    [JsonConverter(typeof(NullableUInt64ToStringConverter))]
    public ulong? OptBLUID { get; set; }

    public string Username { get; set; }
    public string? UserPfpLink { get; set; }
    public string? UserBio { get; set; }

    [JsonConverter(typeof(NullableUInt64ToStringConverter))]
    public ulong? DiscordID { get; set; }
    public string? DiscordUsername { get; set; }

    public string? GoogleID { get; set; }
    public string? GoogleEmail { get; set; }

    public DateTime RegistrationDate { get; set; }
    public string? Role { get; set; }
    public string? FrontendRole { get; set; }

    public string? ScoreSaberProfile { get; set; }
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
        ScoreSaberProfile = reader.IsDBNull(13) ? null : reader.GetString(13);
        BeatLeaderProfile = reader.IsDBNull(14) ? null : reader.GetString(14);
    }

    public async Task<User> UpdateProfilesAsync()
    {
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
            }
        }

        return userProfiles;
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

    public Map(MySqlDataReader reader)
    {
        MID = reader.GetUInt64(0);
        BSMapID = reader.GetString(1);
        MapName = reader.GetString(2);
        MapImageLink = reader.GetString(3);
        MapPoolID = reader.GetUInt64(4);
        Season = reader.GetInt32(5);
        Difficulty = reader.GetString(6);
    }
}

public struct MapPool
{
    public ulong MapPoolID { get; set; }
    public string MapPoolName { get; set; }
    public string MapPoolDescription { get; set; }
    public List<Map> Maps { get; set; }
    public int Season { get; set; }

    public MapPool(MySqlDataReader reader)
    {
        MapPoolID = reader.GetUInt64(0);
        MapPoolName = reader.GetString(1);
        MapPoolDescription = reader.GetString(2);
        Season = reader.GetInt32(3);
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
