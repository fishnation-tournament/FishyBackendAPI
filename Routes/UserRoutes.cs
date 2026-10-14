using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Users/GetUsers", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<FrontFacingUser> users = UserTools.GetUsers(conn).Select(user => new FrontFacingUser(user)).ToList();
            return users;
        }).WithName("GetUsers").WithOpenApi();

        endpoints.MapGet("/Users/GetUserById/{UID}", (ulong UID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            FrontFacingUser user = new FrontFacingUser(UserTools.GetUserById(conn, UID));
            return user;
        }).WithName("GetUserById").WithOpenApi();

        endpoints.MapGet("/Users/SearchUserByName/{searchTerm}", (string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<FrontFacingUser> users = UserTools.GetUsersByName(conn, searchTerm).Select(user => new FrontFacingUser(user)).ToList();
            return users;
        }).WithName("SearchUserByName").WithOpenApi();

        endpoints.MapGet("/Users/GetUserByDiscordID/{DiscordID}", (string DiscordID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            
            ulong parsedID = ulong.Parse(DiscordID);
            FrontFacingUser user = new FrontFacingUser(UserTools.GetUserByDiscordId(conn, parsedID));
            return user;
        }).WithName("GetUserByDiscordID").WithOpenApi();

        endpoints.MapGet("/Users/GetUserByScoresaberID/{SSID}", (ulong SSID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            FrontFacingUser user = new FrontFacingUser(UserTools.GetUserBySSID(conn, SSID));
            return user;
        }).WithName("GetUserByScoresaberID").WithOpenApi();

        endpoints.MapGet("/Users/GetUsersByRole/{Role}", (string Role) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<FrontFacingUser> users = UserTools.GetUsersByRole(conn, Role).Select(user => new FrontFacingUser(user)).ToList();
            return users;
        }).WithName("GetUsersByRole").WithOpenApi().RequirePermission(Permission.UsersViewByRole);

        endpoints.MapGet("/Users/GetSelf", (HttpContext context) =>
        {
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (uidClaim == null)
            {
                return Results.Unauthorized();
            }

            ulong uid = ulong.Parse(uidClaim.Value);

            using MySqlConnection conn = connManager.IssueConnection();
            FrontFacingUser user = new FrontFacingUser(UserTools.GetUserById(conn, uid));
            return Results.Json(user);
        }).WithName("GetSelf").WithOpenApi().RequirePermission(Permission.AccountManageSelf);

        endpoints.MapPut("/Users/UpdateSelf", (HttpContext context, UpdateUser updateUser) =>
        {
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (uidClaim == null)
            {
                Console.WriteLine("[UpdateSelf] Rejected: no uid claim in token.");
                return Results.Unauthorized();
            }

            ulong uid = ulong.Parse(uidClaim.Value);
            Console.WriteLine($"[UpdateSelf] UID={uid} | Username='{updateUser.Username}' | Bio='{updateUser.UserBio}'");

            using MySqlConnection conn = connManager.IssueConnection();
            User user = updateUser.updateUser(UserTools.GetUserById(conn, uid));
            UserTools.UpdateUser(conn, user);

            FrontFacingUser frontFacingUser = new FrontFacingUser(user);
            return Results.Json(frontFacingUser);
        }).WithName("UpdateSelf").WithOpenApi().RequirePermission(Permission.AccountManageSelf);

        endpoints.MapPost("/Users/UpdateProfile", async (HttpContext context) =>
        {
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (uidClaim == null)
            {
                return Results.Unauthorized();
            }

            ulong uid = ulong.Parse(uidClaim.Value);

            using MySqlConnection conn = connManager.IssueConnection();
            User user = await UserTools.GetUserById(conn, uid).UpdateProfilesAsync();
            UserTools.UpdateUser(conn, user);

            FrontFacingUser frontFacingUser = new FrontFacingUser(user);
            return Results.Json(frontFacingUser);
        }).WithName("UpdateProfile").WithOpenApi().RequirePermission(Permission.AccountManageSelf);

        endpoints.MapPost("/Users/AddUser", async (AddUser user) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            User newUser = UserTools.AddUser(conn, await user.UpdateProfilesAsync());
            return Results.Created($"/Users/GetUserById/{newUser.UID}", newUser);
        }).WithName("AddUser").WithOpenApi().RequirePermission(Permission.UsersManage);

        endpoints.MapPut("/Users/UpdateUser", (User user) =>
        {
            Console.WriteLine($"[UpdateUser] UID={user.UID} | Username='{user.Username}' | SSID={user.SSID}");
            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.UpdateUser(conn, user);
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("UpdateUser").WithOpenApi().RequirePermission(Permission.UsersManage);

        // Compatibility shim for the old single-role model: sets the user's admin/organizer/user roles to match
        // "Admin" / "Organizer" / "User" / "Unassigned". Prefer POST/DELETE /Users/{userId}/Roles/{roleKey}.
        endpoints.MapPut("/Users/SetRole/{UID}", (HttpContext context, ulong UID, SetRoleRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Role))
                return Results.BadRequest("Role is required.");

            if (!RoleTools.LegacyRoleValues.Contains(request.Role))
                return Results.BadRequest($"Role must be one of: {string.Join(", ", RoleTools.LegacyRoleValues)}");

            using MySqlConnection conn = connManager.IssueConnection();
            if (UserTools.GetUserById(conn, UID).UID == 0)
                return Results.NotFound("User not found.");
            if (request.Role != "Admin" &&
                RoleTools.GetRoleKeysForUser(conn, UID).Contains("admin") &&
                RoleTools.CountHolders(conn, "admin") <= 1)
                return Results.Conflict("Cannot remove the last admin.");

            RoleTools.ApplyLegacyRole(conn, UID, request.Role, context.GetAccess().UserID);
            FrontFacingUser updated = new FrontFacingUser(UserTools.GetUserById(conn, UID));
            return Results.Ok(updated);
        }).WithName("SetUserRole").WithOpenApi().RequirePermission(Permission.RolesAssign);

        endpoints.MapDelete("/Users/DeleteUser/{UID}", (ulong UID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.DeleteUser(conn, UID);
            return Results.NoContent();
        }).WithName("DeleteUser").WithOpenApi().RequirePermission(Permission.UsersManage);
    }
}
