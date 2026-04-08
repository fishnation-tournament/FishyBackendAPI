using FishyAPI.Tools;
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

        endpoints.MapGet("/Users/GetUserByDiscordID/{DiscordID}", (ulong DiscordID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            FrontFacingUser user = new FrontFacingUser(UserTools.GetUserByDiscordId(conn, DiscordID));
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
        }).WithName("GetUsersByRole").WithOpenApi().RequireAuthorization("User");

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
        }).WithName("GetSelf").WithOpenApi().RequireAuthorization("User");

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
        }).WithName("UpdateSelf").WithOpenApi().RequireAuthorization("User");

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
        }).WithName("UpdateProfile").WithOpenApi().RequireAuthorization("User");

        endpoints.MapPost("/Users/AddUser", async (AddUser user) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            User newUser = UserTools.AddUser(conn, await user.UpdateProfilesAsync());
            return Results.Created($"/Users/GetUserById/{newUser.UID}", newUser);
        }).WithName("AddUser").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Users/UpdateUser", (User user) =>
        {
            Console.WriteLine($"[UpdateUser] UID={user.UID} | Username='{user.Username}' | SSID={user.SSID} | Role='{user.Role}'");
            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.UpdateUser(conn, user);
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("UpdateUser").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Users/SetRole/{UID}", (ulong UID, SetRoleRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Role))
                return Results.BadRequest("Role is required.");

            string[] validRoles = { "Admin", "Organizer", "User", "Unassigned" };
            if (!validRoles.Contains(request.Role))
                return Results.BadRequest($"Role must be one of: {string.Join(", ", validRoles)}");

            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.SetUserRole(conn, UID, request.Role);
            FrontFacingUser updated = new FrontFacingUser(UserTools.GetUserById(conn, UID));
            return Results.Ok(updated);
        }).WithName("SetUserRole").WithOpenApi().RequireAuthorization("Admin");

        endpoints.MapDelete("/Users/DeleteUser/{UID}", (ulong UID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.DeleteUser(conn, UID);
            return Results.NoContent();
        }).WithName("DeleteUser").WithOpenApi().RequireAuthorization("Organizer");
    }
}
