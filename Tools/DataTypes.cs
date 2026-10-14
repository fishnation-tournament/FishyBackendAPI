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
    /// <summary>Role keys the user holds (from user_roles), e.g. ["user", "organizer"].</summary>
    public List<string> Roles { get; set; }
    public string? Title { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(RawJsonStringConverter))]
    public string? ScoreSaberProfile { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(RawJsonStringConverter))]
    public string? BeatLeaderProfile { get; set; }

    /// <summary>Reads the columns selected by UserTools.UserColumns, in that order.</summary>
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
        Title = reader.IsDBNull(11) ? null : reader.GetString(11);
        string? ssProf = reader.IsDBNull(12) ? null : reader.GetString(12);
        string? blProf = reader.IsDBNull(13) ? null : reader.GetString(13);
        Roles = reader.IsDBNull(14) ? new List<string>() : reader.GetString(14).Split(',').ToList();

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
    public int? Season { get; set; }
    public int? Seed { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong? ScoresaberID  { get; set; }
    public FrontFacingUser User { get; set; }

    public Player(MySqlDataReader reader)
    {
        PlayerID = reader.GetInt32(0);
        UID = reader.GetUInt64(1);
        Season = reader.IsDBNull(2) ? null : reader.GetInt32(2);
        Seed =  reader.IsDBNull(3) ? null : reader.GetInt32(3);
        ScoresaberID = reader.IsDBNull(4) ? null : reader.GetUInt64(4); 
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
    public Flair? Flair { get; set; }

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

        int flairIdOrdinal = GetOrdinalOrMinusOne(reader, "FlairID");
        if (flairIdOrdinal >= 0 && !reader.IsDBNull(flairIdOrdinal))
        {
            Flair = new Flair
            {
                FlairID = reader.GetInt32(flairIdOrdinal),
                Color = reader.GetString(reader.GetOrdinal("FlairColor")),
                Name = reader.GetString(reader.GetOrdinal("FlairName"))
            };
        }
        else
        {
            Flair = null;
        }
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
    public PoolType PoolType { get; set; }
    public DateTime? GeneratedAt { get; set; }
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
        PoolType = EnumDbValues.ParsePoolType(reader.GetString(7));
        GeneratedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8);
        Maps = new List<Map>();
    }
}

public struct Match
{
    public ulong MatchID { get; set; }
    public ulong? Player1ID { get; set; }
    public ulong? Player2ID { get; set; }
    public bool Complete { get; set; }
    public ulong? WinnerID { get; set; }
    public int? Player1Score { get; set; }
    public int? Player2Score { get; set; }
    public DateTime? MatchDate { get; set; }
    public List<MatchScore> Scores { get; set; }
    public List<PickBan> PickBans { get; set; }
    public List<MatchLineup> Lineups { get; set; }
    public int Season { get; set; }
    public ulong MapPoolID { get; set; }
    public string? Status { get; set; }
    public int? BestOf { get; set; }
    public bool Player1ReplayUsed { get; set; }
    public bool Player2ReplayUsed { get; set; }
    public bool IsStreamed { get; set; }
    public ulong? CoordinatorUID { get; set; }
    public string? RoundName { get; set; }
    public int? RoundOrder { get; set; }
    public int? BanCount { get; set; }
    public int? PickCount { get; set; }
    public int LocalMatchID { get; set; }
    public bool IsLosersBracket { get; set; }
    public ulong? WinnerGoesToMatch { get; set; }
    public ulong? LoserGoesToMatch { get; set; }
    public int? Team1ID { get; set; }
    public int? Team2ID { get; set; }
    public int? WinnerTeamID { get; set; }
    public FrontFacingDataTypes.FrontFacingUser? Player1User { get; set; }
    public FrontFacingDataTypes.FrontFacingUser? Player2User { get; set; }
    public FrontFacingDataTypes.FrontFacingUser? CoordinatorUser { get; set; }
    public Team? Team1 { get; set; }
    public Team? Team2 { get; set; }

    public Match(MySqlDataReader reader)
    {
        MatchID = reader.GetUInt64(0);
        Player1ID = reader.IsDBNull(1) ? null : reader.GetUInt64(1);
        Player2ID = reader.IsDBNull(2) ? null : reader.GetUInt64(2);
        Player1Score = reader.IsDBNull(3) ? null : reader.GetInt32(3);
        Player2Score = reader.IsDBNull(4) ? null : reader.GetInt32(4);
        MatchDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
        Complete = reader.GetBoolean(6);
        WinnerID = reader.IsDBNull(7) ? null : reader.GetUInt64(7);
        Season = reader.GetInt32(8);
        MapPoolID = reader.GetUInt64(9);
        Status = reader.IsDBNull(10) ? null : reader.GetString(10);
        BestOf = reader.IsDBNull(11) ? null : reader.GetInt32(11);
        Player1ReplayUsed = reader.GetBoolean(12);
        Player2ReplayUsed = reader.GetBoolean(13);
        IsStreamed = reader.GetBoolean(14);
        CoordinatorUID = reader.IsDBNull(15) ? null : reader.GetUInt64(15);
        RoundName = reader.IsDBNull(16) ? null : reader.GetString(16);
        RoundOrder = reader.IsDBNull(17) ? null : reader.GetInt32(17);
        BanCount = reader.IsDBNull(18) ? null : reader.GetInt32(18);
        PickCount = reader.IsDBNull(19) ? null : reader.GetInt32(19);
        LocalMatchID = reader.GetInt32(20);
        IsLosersBracket = reader.GetBoolean(21);
        WinnerGoesToMatch = reader.IsDBNull(22) ? null : reader.GetUInt64(22);
        LoserGoesToMatch = reader.IsDBNull(23) ? null : reader.GetUInt64(23);
        Team1ID = reader.IsDBNull(24) ? null : reader.GetInt32(24);
        Team2ID = reader.IsDBNull(25) ? null : reader.GetInt32(25);
        WinnerTeamID = reader.IsDBNull(26) ? null : reader.GetInt32(26);
        Scores = new List<MatchScore>();
        PickBans = new List<PickBan>();
        Lineups = new List<MatchLineup>();
        Player1User = null;
        Player2User = null;
        CoordinatorUser = null;
        Team1 = null;
        Team2 = null;
    }
}

public struct MatchScore
{
    public ulong ScoreID { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong UID { get; set; }
    public ulong MapID { get; set; }
    public ulong MatchID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public bool ReplayCalledAfter { get; set; }
    public bool IsReplayAttempt { get; set; }
    public int? TeamID { get; set; }

    public MatchScore(MySqlDataReader reader)
    {
        ScoreID = reader.GetUInt64(0);
        UID = reader.GetUInt64(1);
        MapID = reader.GetUInt64(2);
        MatchID = reader.GetUInt64(3);
        Score = reader.GetInt32(4);
        MaxScore = reader.GetInt32(5);
        Misses = reader.GetInt32(6);
        ReplayCalledAfter = reader.GetBoolean(7);
        IsReplayAttempt = reader.GetBoolean(8);
        TeamID = reader.IsDBNull(9) ? null : reader.GetInt32(9);
    }
}

public struct QualifierScore
{
    public ulong ScoreID { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong ScoresaberID { get; set; }
    public ulong MapID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public ulong QualifierID { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Player? Player { get; set; }

    public QualifierScore(MySqlDataReader reader)
    {
        ScoreID = reader.GetUInt64(0);
        ScoresaberID = reader.GetUInt64(1);
        MapID = reader.GetUInt64(2);
        Score = reader.GetInt32(3);
        MaxScore = reader.GetInt32(4);
        Misses = reader.GetInt32(5);
        QualifierID = reader.GetUInt64(6);
        SubmittedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7);
        Player = null;
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
        TokenID = reader.GetValue(0).ToString()!;
        OwnerUID = reader.GetUInt64(1);
        TokenName = reader.GetString(2);
        Role = reader.GetString(3);
        CreatedAt = reader.GetDateTime(4);
        ExpiresAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
        IsRevoked = reader.GetBoolean(6);
    }
}

public struct Season
{
    public int SeasonID { get; set; }
    public string SeasonName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? QualifierCutoff { get; set; }
    public string? DisplayName { get; set; }
    public int SeriesID { get; set; }
    public int? SeasonNumber { get; set; }

    public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
    public bool QualifiersOpen => IsActive && (QualifierCutoff == null || DateTime.UtcNow <= QualifierCutoff);

    public Season(MySqlDataReader reader)
    {
        SeasonID = reader.GetInt32(0);
        SeasonName = reader.GetString(1);
        StartDate = reader.GetDateTime(2);
        EndDate = reader.GetDateTime(3);
        QualifierCutoff = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
        DisplayName = reader.IsDBNull(5) ? null : reader.GetString(5);
        SeriesID = reader.GetInt32(6);
        SeasonNumber = reader.IsDBNull(7) ? null : reader.GetInt32(7);
    }
}

public struct TournamentSeries
{
    public int SeriesID { get; set; }
    public string SeriesName { get; set; }
    public string Slug { get; set; }
    public string? Description { get; set; }

    public TournamentSeries(MySqlDataReader reader)
    {
        SeriesID = reader.GetInt32(0);
        SeriesName = reader.GetString(1);
        Slug = reader.GetString(2);
        Description = reader.IsDBNull(3) ? null : reader.GetString(3);
    }
}

public struct SeasonSettings
{
    public int SeasonID { get; set; }
    public string BracketFormat { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public int? TeamMaxSize { get; set; }
    public int? TeamActivePlayers { get; set; }
    public MapPoolMode MapPoolMode { get; set; }

    public SeasonSettings(MySqlDataReader reader)
    {
        SeasonID = reader.GetInt32(0);
        BracketFormat = reader.GetString(1);
        ParticipantType = EnumDbValues.ParseParticipantType(reader.GetString(2));
        TeamMaxSize = reader.IsDBNull(3) ? null : reader.GetInt32(3);
        TeamActivePlayers = reader.IsDBNull(4) ? null : reader.GetInt32(4);
        MapPoolMode = EnumDbValues.ParseMapPoolMode(reader.GetString(5));
    }
}

public struct Team
{
    public int TeamID { get; set; }
    public int SeasonID { get; set; }
    public string TeamName { get; set; }
    public string? TeamLogoLink { get; set; }
    public int? Seed { get; set; }
    public ulong? CaptainUID { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TeamMember> Members { get; set; }

    public Team(MySqlDataReader reader)
    {
        TeamID = reader.GetInt32(0);
        SeasonID = reader.GetInt32(1);
        TeamName = reader.GetString(2);
        TeamLogoLink = reader.IsDBNull(3) ? null : reader.GetString(3);
        Seed = reader.IsDBNull(4) ? null : reader.GetInt32(4);
        CaptainUID = reader.IsDBNull(5) ? null : reader.GetUInt64(5);
        CreatedAt = reader.GetDateTime(6);
        Members = new List<TeamMember>();
    }
}

public struct TeamMember
{
    public int TeamID { get; set; }
    public int SeasonID { get; set; }
    public ulong UserID { get; set; }
    public DateTime JoinedAt { get; set; }
    public FrontFacingUser? User { get; set; }

    public TeamMember(MySqlDataReader reader)
    {
        TeamID = reader.GetInt32(0);
        SeasonID = reader.GetInt32(1);
        UserID = reader.GetUInt64(2);
        JoinedAt = reader.GetDateTime(3);
        User = null;
    }
}

public struct MatchLineup
{
    public ulong LineupID { get; set; }
    public ulong MatchID { get; set; }
    public ulong MapID { get; set; }
    public int TeamID { get; set; }
    public ulong UserID { get; set; }

    public MatchLineup(MySqlDataReader reader)
    {
        LineupID = reader.GetUInt64(0);
        MatchID = reader.GetUInt64(1);
        MapID = reader.GetUInt64(2);
        TeamID = reader.GetInt32(3);
        UserID = reader.GetUInt64(4);
    }
}

public struct Flair
{
    public int FlairID { get; set; }
    public string Color { get; set; }
    public string Name { get; set; }

    public Flair(MySqlDataReader reader)
    {
        FlairID = reader.GetInt32(0);
        Color = reader.GetString(1);
        Name = reader.GetString(2);
    }
}

public struct PickBan
{
    public int PickBanID { get; set; }
    public ulong MatchID { get; set; }
    public bool IsPick { get; set; }
    public int PlayerID { get; set; }
    public int? TeamID { get; set; }
    public ulong? MapID { get; set; }
    public Map? Map { get; set; }

    public PickBan(MySqlDataReader reader)
    {
        PickBanID = reader.GetInt32(reader.GetOrdinal("PickBanID"));
        MatchID = reader.GetUInt64(reader.GetOrdinal("MatchID"));
        IsPick = reader.GetBoolean(reader.GetOrdinal("Pick"));
        int playerIdOrdinal = reader.GetOrdinal("PlayerID");
        PlayerID = reader.IsDBNull(playerIdOrdinal) ? 0 : reader.GetInt32(playerIdOrdinal);
        int teamIdOrdinal = reader.GetOrdinal("TeamID");
        TeamID = reader.IsDBNull(teamIdOrdinal) ? null : reader.GetInt32(teamIdOrdinal);
        int mapRefOrdinal = reader.GetOrdinal("MapRefID");
        MapID = reader.IsDBNull(mapRefOrdinal) ? null : reader.GetUInt64(mapRefOrdinal);

        int mapIdOrdinal = -1;
        try { mapIdOrdinal = reader.GetOrdinal("MapID"); } catch (IndexOutOfRangeException) { }
        Map = (mapIdOrdinal >= 0 && !reader.IsDBNull(mapIdOrdinal)) ? new Map(reader) : null;
    }
}

public struct MatchSchedule
{
    public int ScheduleID { get; set; }
    public ulong MatchID { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Notes { get; set; }
    public Match? Match { get; set; }

    public MatchSchedule(MySqlDataReader reader)
    {
        ScheduleID = reader.GetInt32(0);
        MatchID = reader.GetUInt64(1);
        ScheduledAt = reader.GetDateTime(2);
        Notes = reader.IsDBNull(3) ? null : reader.GetString(3);
        Match = null;
    }
}

public struct StreamToken
{
    public int UserID { get; set; }
    public string IngressLinkPrimary { get; set; }
    public string IngressLinkSecondary { get; set; }
    public string StreamKey { get; set; }

    public StreamToken(MySqlDataReader reader)
    {
        UserID = reader.GetInt32(0);
        IngressLinkPrimary = reader.GetString(1);
        IngressLinkSecondary = reader.GetString(2);
        StreamKey = reader.GetString(3);
    }
}

public struct AuditLog
{
    public ulong LogID { get; set; }
    public ulong ActorUID { get; set; }
    public string ActorRole { get; set; }
    public string ActionType { get; set; }
    public string ActionContext { get; set; }
    public string? ActionDetail { get; set; }
    public string TokenType { get; set; }
    public DateTime Timestamp { get; set; }

    public AuditLog(MySqlDataReader reader)
    {
        LogID = reader.GetUInt64(0);
        ActorUID = reader.GetUInt64(1);
        ActorRole = reader.GetString(2);
        ActionType = reader.GetString(3);
        ActionContext = reader.GetString(4);
        ActionDetail = reader.IsDBNull(5) ? null : reader.GetString(5);
        TokenType = reader.GetString(6);
        Timestamp = reader.GetDateTime(7);
    }
}

public struct RoleDefinition
{
    public int RoleID { get; set; }
    public string RoleKey { get; set; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }

    public RoleDefinition(MySqlDataReader reader)
    {
        RoleID = reader.GetInt32(0);
        RoleKey = reader.GetString(1);
        DisplayName = reader.GetString(2);
        Description = reader.IsDBNull(3) ? null : reader.GetString(3);
        Color = reader.IsDBNull(4) ? null : reader.GetString(4);
        IsSystem = reader.GetBoolean(5);
        CreatedAt = reader.GetDateTime(6);
    }
}

public struct UserRoleAssignment
{
    public RoleDefinition Role { get; set; }
    public DateTime AssignedAt { get; set; }
    public ulong? AssignedByUID { get; set; }

    public UserRoleAssignment(MySqlDataReader reader)
    {
        Role = new RoleDefinition(reader);
        AssignedAt = reader.GetDateTime(7);
        AssignedByUID = reader.IsDBNull(8) ? null : reader.GetUInt64(8);
    }
}
