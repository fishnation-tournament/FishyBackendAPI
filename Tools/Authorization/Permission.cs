using System.Text.Json.Serialization;

namespace FishyAPI.Tools.Authorization;

/// <summary>
/// Everything the API can authorize. Endpoints require one of these; roles are granted them in AccessPolicy.
/// Adding a value here does not grant it to anyone except Admin.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Permission>))]
public enum Permission
{
    // ── Own account ──────────────────────────────────────────────────────────
    /// <summary>View and edit your own profile, refresh ScoreSaber/BeatLeader data, link Steam.</summary>
    AccountManageSelf,
    /// <summary>Register yourself as a player for the active season.</summary>
    PlayersRegisterSelf,
    /// <summary>Read your own stream key.</summary>
    StreamKeyReadOwn,
    /// <summary>Look up users by role.</summary>
    UsersViewByRole,

    // ── API tokens ───────────────────────────────────────────────────────────
    /// <summary>Issue, list and revoke your own API tokens.</summary>
    ApiTokensManageOwn,
    /// <summary>List and revoke anyone's API tokens.</summary>
    ApiTokensManageAll,

    // ── Maps ─────────────────────────────────────────────────────────────────
    /// <summary>See non-public map pools and the maps in them.</summary>
    MapPoolsViewPrivate,
    /// <summary>Create, edit and delete map pools.</summary>
    MapPoolsManage,
    /// <summary>Add, edit and delete maps.</summary>
    MapsManage,
    /// <summary>Create, edit and delete flairs, and assign them to maps.</summary>
    FlairsManage,

    // ── Matches ──────────────────────────────────────────────────────────────
    /// <summary>Create and delete matches (building the bracket).</summary>
    MatchesManage,
    /// <summary>Edit match details and results, call replays.</summary>
    MatchesUpdate,
    /// <summary>Add and edit match scores.</summary>
    MatchScoresManage,
    /// <summary>Delete match scores.</summary>
    MatchScoresDelete,
    /// <summary>Add and remove picks and bans.</summary>
    PickBansManage,
    /// <summary>Set and remove team lineups.</summary>
    LineupsManage,
    /// <summary>Create and remove match schedule entries.</summary>
    ScheduleManage,
    /// <summary>
    /// Correct a match's records directly (picks/bans, scores, replays) outside the normal turn order.
    /// Results are still recomputed from the records, never set by hand.
    /// </summary>
    MatchesOverride,

    // ── Tournament ───────────────────────────────────────────────────────────
    /// <summary>Create, edit and delete teams and their rosters.</summary>
    TeamsManage,
    /// <summary>Add, edit and remove players.</summary>
    PlayersManage,
    /// <summary>Add and remove qualifier scores.</summary>
    QualifierScoresManage,
    /// <summary>Create, edit and delete seasons and season settings.</summary>
    SeasonsManage,
    /// <summary>Create, edit and delete tournament series.</summary>
    SeriesManage,

    // ── Administration ───────────────────────────────────────────────────────
    /// <summary>Create, edit and delete user accounts (not their roles).</summary>
    UsersManage,
    /// <summary>Create, edit and delete roles.</summary>
    RolesManage,
    /// <summary>Give roles to and take roles from users.</summary>
    RolesAssign,
    /// <summary>Manage everyone's stream tokens.</summary>
    StreamTokensManage,
    /// <summary>Read the audit log.</summary>
    AuditLogRead,
}
