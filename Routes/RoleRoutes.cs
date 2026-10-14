using System.Text.RegularExpressions;
using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class RoleRoutes
{
    private static readonly Regex RoleKeyPattern = new("^[a-z][a-z0-9_]{1,49}$", RegexOptions.Compiled);
    private static readonly Regex ColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private static RoleWithPermissions WithPermissions(RoleDefinition role) => new()
    {
        Role = role,
        Permissions = AccessPolicy.GrantsFor(role.RoleKey).Order().ToList()
    };

    public static void MapRoleRoutes(WebApplication app, ConnectionManager connManager)
    {
        // ── Caller's own access ──────────────────────────────────────────────

        app.MapGet("/Auth/Permissions", (HttpContext context) =>
        {
            UserAccess access = context.GetAccess();
            return Results.Ok(new AccessResponse
            {
                UserID = access.UserID,
                Roles = access.Roles,
                Permissions = access.Permissions.Order().ToList()
            });
        }).WithName("GetOwnPermissions").WithOpenApi().RequireAuthorization();

        // ── Role definitions ─────────────────────────────────────────────────

        app.MapGet("/Roles", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(RoleTools.GetRoles(conn).Select(WithPermissions).ToList());
        }).WithName("GetRoles").WithOpenApi();

        app.MapGet("/Roles/{roleKey}", (string roleKey) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            RoleDefinition? role = RoleTools.GetRoleByKey(conn, roleKey);
            return role.HasValue ? Results.Ok(WithPermissions(role.Value)) : Results.NotFound();
        }).WithName("GetRole").WithOpenApi();

        app.MapPost("/Roles", (CreateRoleRequest request) =>
        {
            if (request.RoleKey == null || !RoleKeyPattern.IsMatch(request.RoleKey))
                return Results.BadRequest("RoleKey must be 2-50 characters: lowercase letters, digits and underscores, starting with a letter (e.g. 'map_pooler').");
            if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 100)
                return Results.BadRequest("DisplayName is required and must be at most 100 characters.");
            if (request.Color != null && !ColorPattern.IsMatch(request.Color))
                return Results.BadRequest("Color must be in #RRGGBB format.");

            using MySqlConnection conn = connManager.IssueConnection();
            try
            {
                RoleDefinition created = RoleTools.CreateRole(conn, request);
                return Results.Created($"/Roles/{created.RoleKey}", WithPermissions(created));
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict($"A role with key '{request.RoleKey}' already exists.");
            }
        }).WithName("CreateRole").WithOpenApi().RequirePermission(Permission.RolesManage);

        app.MapPut("/Roles/{roleKey}", (string roleKey, UpdateRoleRequest request) =>
        {
            if (request.DisplayName != null && (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 100))
                return Results.BadRequest("DisplayName must be 1-100 characters.");
            if (request.Color != null && !ColorPattern.IsMatch(request.Color))
                return Results.BadRequest("Color must be in #RRGGBB format.");

            using MySqlConnection conn = connManager.IssueConnection();
            RoleDefinition? updated = RoleTools.UpdateRole(conn, roleKey, request);
            return updated.HasValue ? Results.Ok(WithPermissions(updated.Value)) : Results.NotFound();
        }).WithName("UpdateRole").WithOpenApi().RequirePermission(Permission.RolesManage);

        app.MapDelete("/Roles/{roleKey}", (string roleKey) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            RoleDefinition? role = RoleTools.GetRoleByKey(conn, roleKey);
            if (!role.HasValue) return Results.NotFound();
            if (role.Value.IsSystem)
                return Results.Conflict($"'{role.Value.RoleKey}' is a system role the API grants permissions to and cannot be deleted.");

            RoleTools.DeleteRole(conn, role.Value.RoleID);
            return Results.NoContent();
        }).WithName("DeleteRole").WithOpenApi().RequirePermission(Permission.RolesManage);

        // ── Assignments ──────────────────────────────────────────────────────

        app.MapGet("/Users/{userId}/Roles", (ulong userId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            if (UserTools.GetUserById(conn, userId).UID == 0) return Results.NotFound("User not found.");
            return Results.Ok(RoleTools.GetUserRoles(conn, userId));
        }).WithName("GetUserRoles").WithOpenApi();

        app.MapPost("/Users/{userId}/Roles/{roleKey}", (HttpContext context, ulong userId, string roleKey) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            if (UserTools.GetUserById(conn, userId).UID == 0) return Results.NotFound("User not found.");
            if (!RoleTools.AssignRole(conn, userId, roleKey, context.GetAccess().UserID))
                return Results.NotFound($"Role '{roleKey}' does not exist.");
            return Results.Ok(RoleTools.GetUserRoles(conn, userId));
        }).WithName("AssignUserRole").WithOpenApi().RequirePermission(Permission.RolesAssign);

        app.MapDelete("/Users/{userId}/Roles/{roleKey}", (ulong userId, string roleKey) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            if (string.Equals(roleKey, "admin", StringComparison.OrdinalIgnoreCase) &&
                RoleTools.GetRoleKeysForUser(conn, userId).Contains("admin") &&
                RoleTools.CountHolders(conn, "admin") <= 1)
                return Results.Conflict("Cannot remove the last admin.");

            bool removed = RoleTools.RemoveRole(conn, userId, roleKey);
            return removed ? Results.Ok(RoleTools.GetUserRoles(conn, userId)) : Results.NotFound();
        }).WithName("RemoveUserRole").WithOpenApi().RequirePermission(Permission.RolesAssign);
    }
}
