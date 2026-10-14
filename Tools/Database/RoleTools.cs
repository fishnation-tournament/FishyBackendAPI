using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public static class RoleTools
{
    private const string RoleColumns = "r.RoleID, r.RoleKey, r.DisplayName, r.Description, r.Color, r.IsSystem, r.CreatedAt";

    /// <summary>Old single-role values (used by /Users/SetRole) and the role each maps to. Every member also holds 'user'.</summary>
    private static readonly Dictionary<string, string> LegacyTierRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = "admin",
        ["Organizer"] = "organizer",
        ["User"] = "user",
    };

    public static readonly string[] LegacyRoleValues = ["Admin", "Organizer", "User", "Unassigned"];

    private static List<RoleDefinition> ReadRoles(MySqlCommand cmd)
    {
        List<RoleDefinition> roles = new();
        using MySqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
            roles.Add(new RoleDefinition(reader));
        return roles;
    }

    public static List<string> GetRoleKeysForUser(MySqlConnection conn, ulong userId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT r.RoleKey FROM user_roles ur JOIN roles r ON r.RoleID = ur.RoleID WHERE ur.UserID = @UserID ORDER BY r.RoleID",
            conn);
        cmd.Parameters.AddWithValue("@UserID", userId);
        List<string> keys = new();
        using MySqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
            keys.Add(reader.GetString(0));
        return keys;
    }

    public static List<RoleDefinition> GetRoles(MySqlConnection conn)
        => ReadRoles(new MySqlCommand($"SELECT {RoleColumns} FROM roles r ORDER BY r.RoleID", conn));

    public static RoleDefinition? GetRoleByKey(MySqlConnection conn, string roleKey)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {RoleColumns} FROM roles r WHERE r.RoleKey = @RoleKey LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@RoleKey", roleKey);
        List<RoleDefinition> roles = ReadRoles(cmd);
        return roles.Count == 0 ? null : roles[0];
    }

    public static RoleDefinition CreateRole(MySqlConnection conn, CreateRoleRequest request)
    {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO roles (RoleKey, DisplayName, Description, Color) VALUES (@RoleKey, @DisplayName, @Description, @Color)",
            conn);
        cmd.Parameters.AddWithValue("@RoleKey", request.RoleKey);
        cmd.Parameters.AddWithValue("@DisplayName", request.DisplayName);
        cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Color", request.Color ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
        return GetRoleByKey(conn, request.RoleKey)!.Value;
    }

    /// <summary>RoleKey is deliberately not editable: code grants permissions by key.</summary>
    public static RoleDefinition? UpdateRole(MySqlConnection conn, string roleKey, UpdateRoleRequest request)
    {
        RoleDefinition? existing = GetRoleByKey(conn, roleKey);
        if (!existing.HasValue) return null;

        MySqlCommand cmd = new MySqlCommand(
            "UPDATE roles SET DisplayName = @DisplayName, Description = @Description, Color = @Color WHERE RoleID = @RoleID",
            conn);
        cmd.Parameters.AddWithValue("@DisplayName", request.DisplayName ?? existing.Value.DisplayName);
        cmd.Parameters.AddWithValue("@Description",
            request.ClearDescription ? DBNull.Value : (object?)(request.Description ?? existing.Value.Description) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Color",
            request.ClearColor ? DBNull.Value : (object?)(request.Color ?? existing.Value.Color) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RoleID", existing.Value.RoleID);
        cmd.ExecuteNonQuery();
        return GetRoleByKey(conn, roleKey);
    }

    /// <summary>Deletes a role and all of its assignments.</summary>
    public static bool DeleteRole(MySqlConnection conn, int roleId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM roles WHERE RoleID = @RoleID", conn);
        cmd.Parameters.AddWithValue("@RoleID", roleId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public static List<UserRoleAssignment> GetUserRoles(MySqlConnection conn, ulong userId)
    {
        MySqlCommand cmd = new MySqlCommand($"""
            SELECT {RoleColumns}, ur.AssignedAt, ur.AssignedByUID
            FROM user_roles ur
            JOIN roles r ON r.RoleID = ur.RoleID
            WHERE ur.UserID = @UserID
            ORDER BY r.RoleID
            """, conn);
        cmd.Parameters.AddWithValue("@UserID", userId);
        List<UserRoleAssignment> assignments = new();
        using MySqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
            assignments.Add(new UserRoleAssignment(reader));
        return assignments;
    }

    public static int CountHolders(MySqlConnection conn, string roleKey)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM user_roles ur JOIN roles r ON r.RoleID = ur.RoleID WHERE r.RoleKey = @RoleKey",
            conn);
        cmd.Parameters.AddWithValue("@RoleKey", roleKey);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Gives the role to the user. Returns false if the role doesn't exist. Already holding it is fine.</summary>
    public static bool AssignRole(MySqlConnection conn, ulong userId, string roleKey, ulong? assignedBy)
    {
        MySqlCommand cmd = new MySqlCommand("""
            INSERT INTO user_roles (UserID, RoleID, AssignedByUID)
            SELECT @UserID, RoleID, @AssignedBy FROM roles WHERE RoleKey = @RoleKey
            ON DUPLICATE KEY UPDATE UserID = user_roles.UserID
            """, conn);
        cmd.Parameters.AddWithValue("@UserID", userId);
        cmd.Parameters.AddWithValue("@RoleKey", roleKey);
        cmd.Parameters.AddWithValue("@AssignedBy", assignedBy ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
        return GetRoleByKey(conn, roleKey).HasValue;
    }

    public static bool RemoveRole(MySqlConnection conn, ulong userId, string roleKey)
    {
        MySqlCommand cmd = new MySqlCommand("""
            DELETE ur FROM user_roles ur
            JOIN roles r ON r.RoleID = ur.RoleID
            WHERE ur.UserID = @UserID AND r.RoleKey = @RoleKey
            """, conn);
        cmd.Parameters.AddWithValue("@UserID", userId);
        cmd.Parameters.AddWithValue("@RoleKey", roleKey);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Applies an old single-role value (used by /Users/SetRole):
    /// Admin -> admin + user, Organizer -> organizer + user, User -> user, Unassigned -> none of the three.
    /// Other roles the user holds (player, map_pooler, ...) are untouched.
    /// </summary>
    public static void ApplyLegacyRole(MySqlConnection conn, ulong userId, string? legacyRole, ulong? assignedBy)
    {
        HashSet<string> wanted = new(StringComparer.OrdinalIgnoreCase);
        if (legacyRole != null && LegacyTierRoles.TryGetValue(legacyRole, out string? tier))
        {
            wanted.Add(tier);
            wanted.Add("user");
        }

        foreach (string tierKey in LegacyTierRoles.Values)
        {
            if (wanted.Contains(tierKey))
                AssignRole(conn, userId, tierKey, assignedBy);
            else
                RemoveRole(conn, userId, tierKey);
        }
    }

    public static List<string> MissingRoleKeys(MySqlConnection conn, IEnumerable<string> roleKeys)
    {
        HashSet<string> existing = GetRoles(conn).Select(r => r.RoleKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roleKeys.Where(k => !existing.Contains(k)).ToList();
    }
}
