using System.Text.Json.Serialization;
using System.Xml;
using FishyAPI.Tools.DataTypes;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace FishyAPI.Tools.FrontFacingDataTypes;

public struct FrontFacingUser
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

    public DateTime RegistrationDate { get; set; }
    public List<string> Roles { get; set; }
    public string? Title { get; set; }

    [JsonConverter(typeof(RawJsonStringConverter))]
    public string? ScoreSaberProfile { get; set; }

    [JsonConverter(typeof(RawJsonStringConverter))]
    public string? BeatLeaderProfile { get; set; }

    public FrontFacingUser(DataTypes.User user)
    {
        UID = user.UID;
        SSID = user.SSID;
        OptBLUID = user.OptBLUID;
        Username = user.Username;
        UserPfpLink = user.UserPfpLink;
        UserBio = user.UserBio;
        DiscordID = user.DiscordID;
        DiscordUsername = user.DiscordUsername;
        RegistrationDate = user.RegistrationDate;
        Roles = user.Roles;
        Title = user.Title;
        ScoreSaberProfile = user.ScoreSaberProfile;
        BeatLeaderProfile = user.BeatLeaderProfile;
    }
}

public struct AddUser
{
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

    public DateTime RegistrationDate { get; set; }
    public string? Title { get; set; }

    [JsonConverter(typeof(RawJsonStringConverter))]
    public string? ScoreSaberProfile { get; set; }

    [JsonConverter(typeof(RawJsonStringConverter))]
    public string? BeatLeaderProfile { get; set; }

    public async Task<AddUser> UpdateProfilesAsync()
    {
        DataTypes.UserProfiles userProfiles = await DataTypes.UserProfiles.GetUserProfilesAsync(SSID, OptBLUID);
        ScoreSaberProfile = userProfiles.ScoreSaberProfile;
        BeatLeaderProfile = userProfiles.BeatLeaderProfile;

        return this;
    }
}

public struct UpdateUser
{
    public string? Username { get; set; }
    public string? UserBio { get; set; }

    public DataTypes.User updateUser(DataTypes.User user)
    {
        if (Username != null)
        {
            user.Username = Username;
        }

        if (UserBio != null)
        {
            user.UserBio = UserBio;
        }

        return user;
    }
}

public struct AddPlayer
{
    public ulong UID { get; set; }
    public int Season { get; set; }
    public int? Seed { get; set; }
}

public struct FrontFacingMap
{
    public string BSMapID { get; set; }
    public string MapName { get; set; }
    public string MapImageLink { get; set; }
    public ulong MapPoolID { get; set; }
    public int Season { get; set; }
    public string Difficulty { get; set; }
    public List<string> Tags { get; set; }
}

public struct FrontFacingMapPool
{
    public string MapPoolName { get; set; }
    public string MapPoolDescription { get; set; }
    public int Season { get; set; }
    public string DownloadLink { get; set; }
    public string OneClickLink { get; set; }
    public string Visibility { get; set; }
    /// <summary>Defaults to Season when omitted.</summary>
    public PoolType? PoolType { get; set; }
    public DateTime? GeneratedAt { get; set; }
    /// <summary>Optional. Saved into the new pool; each map's MapPoolID and Season are taken from the pool.</summary>
    public List<FrontFacingMap>? Maps { get; set; }
}

public struct FrontFacingMatch
{
    public ulong MapPoolID { get; set; }
    public ulong? Player1ID { get; set; }
    public ulong? Player2ID { get; set; }
    public bool Complete { get; set; }
    public ulong? WinnerID { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public DateTime? MatchDate { get; set; }
    public List<FrontFacingMatchScore> Scores { get; set; }
    public int Season { get; set; }
    public string? Status { get; set; }
    public int? BestOf { get; set; }
    public bool IsStreamed { get; set; }
    public ulong? CoordinatorUID { get; set; }
    public string? RoundName { get; set; }
    public int? RoundOrder { get; set; }
    public int? BanCount { get; set; }
    public int? PickCount { get; set; }
    public int LocalMatchID  { get; set; }
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool IsLosersBracket { get; set; }
    public ulong? WinnerGoesToMatch { get; set; }
    public ulong? LoserGoesToMatch  { get; set; }
    public int? Team1ID { get; set; }
    public int? Team2ID { get; set; }
    public int? WinnerTeamID { get; set; }
}

public struct UpdateMatchScoreRequest
{
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
}

public struct CallReplayRequest
{
    /// <summary>UID of the player calling the replay. Must be Player1 or Player2 of the match.</summary>
    [System.Text.Json.Serialization.JsonNumberHandling(System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString)]
    public ulong CallingPlayerUID { get; set; }
}

public struct BulkCreateMatchEntry
{
    public ulong MapPoolID { get; set; }
    public ulong? Player1ID { get; set; }
    public ulong? Player2ID { get; set; }
    public bool Complete { get; set; }
    public ulong? WinnerID { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public DateTime? MatchDate { get; set; }
    public int Season { get; set; }
    public string? Status { get; set; }
    public int? BestOf { get; set; }
    public bool IsStreamed { get; set; }
    public ulong? CoordinatorUID { get; set; }
    public string? RoundName { get; set; }
    public int? RoundOrder { get; set; }
    public int? BanCount { get; set; }
    public int? PickCount { get; set; }
    public int LocalMatchID { get; set; }
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool IsLosersBracket { get; set; }
    public ulong? WinnerGoesToMatch { get; set; }
    public ulong? LoserGoesToMatch { get; set; }
    public int? Team1ID { get; set; }
    public int? Team2ID { get; set; }
    public int? WinnerTeamID { get; set; }
    // Optional schedule — omit ScheduledAt to skip creating a schedule entry
    public DateTime? ScheduledAt { get; set; }
    public string? ScheduleNotes { get; set; }
    
}

public struct BulkMatchCreationResult
{
    public DataTypes.Match Match { get; set; }
    public DataTypes.MatchSchedule? Schedule { get; set; }
}

public struct CreatePickBanRequest
{
    public ulong MatchID { get; set; }
    /// <summary>true = pick, false = ban</summary>
    public bool IsPick { get; set; }
    /// <summary>Acting player for solo matches. Optional when TeamID is given.</summary>
    public int? PlayerID { get; set; }
    /// <summary>Acting team for team matches.</summary>
    public int? TeamID { get; set; }
    public ulong MapID { get; set; }
}

public struct CreateMatchScheduleRequest
{
    public ulong MatchID { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Notes { get; set; }
}

public struct FrontFacingMatchScore
{
    [JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong UID { get; set; }
    public ulong MapID { get; set; }
    public ulong MatchID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public bool IsReplayAttempt { get; set; }
    public int? TeamID { get; set; }
}

public struct AddQualifierScore
{
    public ulong MapID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public ulong QualifierID { get; set; }
    [JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong ScoresaberID { get; set; }
}

public struct FrontFacingQualifierScore
{
    public ulong MapID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public ulong QualifierID { get; set; }
    [JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong ScoresaberID { get; set; }
    public Player? scorePlayer { get; set; }

    public FrontFacingQualifierScore(AddQualifierScore score)
    {
        ScoresaberID = score.ScoresaberID;
        MapID = score.MapID;
        Score = score.Score;
        MaxScore = score.MaxScore;
        Misses = score.Misses;
        QualifierID = score.QualifierID;
        scorePlayer = null;
    }
}

public struct SetRoleRequest
{
    public string Role { get; set; }
}

public struct CreateSeasonRequest
{
    public string SeasonName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? QualifierCutoff { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>Defaults to 1 when omitted.</summary>
    public int? SeriesID { get; set; }
    public int? SeasonNumber { get; set; }
}

public struct UpdateSeasonRequest
{
    public string? SeasonName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? QualifierCutoff { get; set; }
    public bool ClearQualifierCutoff { get; set; }
    public string? DisplayName { get; set; }
    public bool ClearDisplayName { get; set; }
    public int? SeriesID { get; set; }
    public int? SeasonNumber { get; set; }
    public bool ClearSeasonNumber { get; set; }
}

public struct CreateTournamentSeriesRequest
{
    public string SeriesName { get; set; }
    public string Slug { get; set; }
    public string? Description { get; set; }
}

public struct UpdateTournamentSeriesRequest
{
    public string? SeriesName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
}

public struct UpsertSeasonSettingsRequest
{
    public string? BracketFormat { get; set; }
    public ParticipantType? ParticipantType { get; set; }
    public int? TeamMaxSize { get; set; }
    public int? TeamActivePlayers { get; set; }
    public MapPoolMode? MapPoolMode { get; set; }
}

public struct CreateTeamRequest
{
    public int SeasonID { get; set; }
    public string TeamName { get; set; }
    public string? TeamLogoLink { get; set; }
    public int? Seed { get; set; }
    public ulong? CaptainUID { get; set; }
}

public struct UpdateTeamRequest
{
    public string? TeamName { get; set; }
    public string? TeamLogoLink { get; set; }
    public bool ClearTeamLogoLink { get; set; }
    public int? Seed { get; set; }
    public bool ClearSeed { get; set; }
    public ulong? CaptainUID { get; set; }
    public bool ClearCaptain { get; set; }
}

public struct AddTeamMemberRequest
{
    public ulong UserID { get; set; }
}

public struct CreateMatchLineupRequest
{
    public ulong MatchID { get; set; }
    public ulong MapID { get; set; }
    public int TeamID { get; set; }
    public ulong UserID { get; set; }
}

public struct CreateFlairRequest
{
    public string Color { get; set; }
    public string Name { get; set; }
}

public struct UpdateFlairRequest
{
    public string? Color { get; set; }
    public string? Name { get; set; }
}

public struct AddStreamTokenRequest
{
    public int UserID { get; set; }
    public string IngressLinkPrimary { get; set; }
    public string IngressLinkSecondary { get; set; }
    public string StreamKey { get; set; }
}

public struct UpdateStreamTokenRequest
{
    public string? IngressLinkPrimary { get; set; }
    public string? IngressLinkSecondary { get; set; }
    public string? StreamKey { get; set; }
}

public struct IssueApiTokenRequest
{
    public string TokenName { get; set; }
    public string Role { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public struct IssuedApiTokenResponse
{
    public string TokenID { get; set; }
    public string TokenName { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Token { get; set; }
}

public struct CreateRoleRequest
{
    /// <summary>Lowercase identifier, e.g. "map_pooler". Cannot be changed later.</summary>
    public string RoleKey { get; set; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
}

public struct UpdateRoleRequest
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool ClearDescription { get; set; }
    public string? Color { get; set; }
    public bool ClearColor { get; set; }
}

/// <summary>A role plus the permissions AccessPolicy grants it.</summary>
public struct RoleWithPermissions
{
    public DataTypes.RoleDefinition Role { get; set; }
    public List<Authorization.Permission> Permissions { get; set; }
}

public struct AccessResponse
{
    public ulong? UserID { get; set; }
    public IReadOnlyList<string> Roles { get; set; }
    public List<Authorization.Permission> Permissions { get; set; }
}

// ── Guarded match actions (see Routes/MatchFlowRoutes.cs) ────────────────────

public struct MatchVersionedAction
{
    /// <summary>The version from the match state you acted on.</summary>
    public string? ExpectedVersion { get; set; }
}

public struct MatchPickBanAction
{
    public ulong MapID { get; set; }
    public string? ExpectedVersion { get; set; }
}

public struct MatchScoreAction
{
    /// <summary>The player's user ID (not their ScoreSaber ID).</summary>
    public ulong UserID { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    public string? ExpectedVersion { get; set; }
}

public struct MatchReplayAction
{
    /// <summary>Optional: the side you expect to be calling it (rejected if it isn't theirs to call).</summary>
    public int? Side { get; set; }
    public string? ExpectedVersion { get; set; }
}

public struct MatchTiebreakerAction
{
    public ulong MapID { get; set; }
    public string? ExpectedVersion { get; set; }
}

public struct MatchForfeitAction
{
    /// <summary>The side (1 or 2) that forfeits.</summary>
    public int Side { get; set; }
    public string? ExpectedVersion { get; set; }
}
