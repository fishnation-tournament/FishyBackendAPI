using FishyAPI.Tools;
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
            List<DataTypes.User> users = UserTools.GetUsers(conn);
            return users;
        }).WithName("GetUsers").WithOpenApi();

        endpoints.MapGet("/Users/GetUserById/{UID}", (ulong UID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.User user = UserTools.GetUserById(conn, UID);
            return user;
        }).WithName("GetUserById").WithOpenApi();

        endpoints.MapGet("/Users/SearchUserByName/{searchTerm}", (string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.User> users = UserTools.GetUsersByName(conn, searchTerm);
            return users;
        }).WithName("SearchUserByName").WithOpenApi();

        endpoints.MapGet("/Users/GetUserByDiscordID/{DiscordID}", (ulong DiscordID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.User user = UserTools.GetUserByDiscordId(conn, DiscordID);
            return user;
        }).WithName("GetUserByDiscordID").WithOpenApi();

        endpoints.MapGet("/Users/GetUsersByRole/{Role}", (string Role) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.User> users = UserTools.GetUsersByRole(conn, Role);
            return users;
        }).WithName("GetUsersByRole").WithOpenApi().RequireAuthorization("User");

        endpoints.MapGet("/Users/GetSelf", (HttpContext context) =>
        {
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");

            ulong uid = ulong.Parse(uidClaim.Value);

            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.User user = UserTools.GetUserById(conn, uid);
            return Results.Json(user);
        }).WithName("GetSelf").WithOpenApi().RequireAuthorization("User");

        endpoints.MapPost("/Users/AddUser", (FrontFacingDataTypes.FrontFacingUser user) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.User newUser = UserTools.AddUser(conn, user);
            return Results.Created($"/Users/GetUserById/{newUser.UID}", newUser);
        }).WithName("AddUser").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Users/UpdateUser", (DataTypes.User user) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.UpdateUser(conn, user);
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("UpdateUser").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Users/DeleteUser/{UID}", (ulong UID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            UserTools.DeleteUser(conn, UID);
            return Results.NoContent();
        }).WithName("DeleteUser").WithOpenApi().RequireAuthorization("Organizer");
    }
}