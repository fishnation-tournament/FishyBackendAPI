using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;

namespace FishyAPI.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Users/GetUsers", () =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.User> users = UserTools.GetUsers(interactionHelper);
            conn.Close();
            Console.WriteLine(users[0].SSID);
            return users;
        }).WithName("GetUsers").WithOpenApi();

        endpoints.MapGet("/Users/GetUserById/{UID}", (ulong UID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.User user = UserTools.GetUserById(interactionHelper, UID);
            conn.Close();
            return user;
        }).WithName("GetUserById").WithOpenApi();

        endpoints.MapGet("/Users/SearchUserByName/{searchTerm}", (string searchTerm) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.User> users = UserTools.GetUsersByName(interactionHelper, searchTerm);
            conn.Close();
            return users;
        }).WithName("SearchUserByName").WithOpenApi();

        endpoints.MapGet("/Users/GetUserByDiscordID/{DiscordID}", (ulong DiscordID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.User user = UserTools.GetUserByDiscordId(interactionHelper, DiscordID);
            conn.Close();
            return user;
        }).WithName("GetUserByDiscordID").WithOpenApi();

        endpoints.MapGet("/Users/GetUsersByRole/{Role}", (string Role) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.User> users = UserTools.GetUsersByRole(interactionHelper, Role);
            conn.Close();
            return users;
        }).WithName("GetUsersByRole").WithOpenApi().RequireAuthorization("User");
        
        endpoints.MapGet("/Users/GetSelf", (HttpContext context) =>
        {
            if(context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return Results.Unauthorized();
            }
            
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");
            if(uidClaim == null)
            {
                return Results.Unauthorized();
            }

            ulong uid = ulong.Parse(uidClaim.Value);
            
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.User user = UserTools.GetUserById(interactionHelper, uid);
            conn.Close();
            return Results.Json(user);
        }).WithName("GetSelf").WithOpenApi().RequireAuthorization("User");
        
        endpoints.MapPost("/Users/AddUser", (DataTypes.User user) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            UserTools.AddUser(interactionHelper, user);
            conn.Close();
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("AddUser").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapPut("/Users/UpdateUser", (DataTypes.User user) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            UserTools.UpdateUser(interactionHelper, user);
            conn.Close();
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("UpdateUser").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapDelete("/Users/DeleteUser/{UID}", (ulong UID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            UserTools.DeleteUser(interactionHelper, UID);
            conn.Close();
            return Results.NoContent();
        }).WithName("DeleteUser").WithOpenApi().RequireAuthorization("Organizer");
    }
}