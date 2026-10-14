using static FishyAPI.Tools.Authorization.Permission;

namespace FishyAPI.Tools.Authorization;

/// <summary>
/// The single place that decides what each role may do.
///
/// Which roles exist, and who holds them, lives in the database (roles / user_roles).
/// A user's permissions are the union of the grants below for every role they hold;
/// a role in the database with no entry here grants nothing.
/// API tokens get the grants of the role they were issued for, limited to what their owner holds now.
/// </summary>
public static class AccessPolicy
{
    // Shared building blocks.
    private static readonly Permission[] Member = [AccountManageSelf, PlayersRegisterSelf, StreamKeyReadOwn, UsersViewByRole];
    private static readonly Permission[] Staff = [ApiTokensManageOwn];
    private static readonly Permission[] MatchDay = [MatchesUpdate, MatchScoresManage, PickBansManage, LineupsManage, ScheduleManage];
    private static readonly Permission[] MapPooling = [MapPoolsViewPrivate, MapPoolsManage, MapsManage, FlairsManage];

    // ── Grants ───────────────────────────────────────────────────────────────
    private static readonly Dictionary<string, Permission[]> Grants = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user"]        = [.. Member],
        ["player"]      = [],
        ["caster"]      = [],
        ["map_pooler"]  = [.. Staff, .. MapPooling],
        ["coordinator"] = [.. Staff, .. MatchDay, MapPoolsViewPrivate],
        ["organizer"]   = [.. Staff, .. MatchDay, .. MapPooling, MatchesManage, TeamsManage, PlayersManage, UsersManage],
        ["admin"]       = Enum.GetValues<Permission>(),
    };
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Role keys that have grants in code; these must exist in the roles table.</summary>
    public static IEnumerable<string> RolesWithGrants => Grants.Keys;

    public static IReadOnlySet<Permission> GrantsFor(string roleKey)
        => Grants.TryGetValue(roleKey, out Permission[]? perms) ? perms.ToHashSet() : new HashSet<Permission>();

    public static HashSet<Permission> PermissionsFor(IEnumerable<string> roleKeys)
    {
        HashSet<Permission> result = new();
        foreach (string key in roleKeys)
            result.UnionWith(GrantsFor(key));
        return result;
    }
}
