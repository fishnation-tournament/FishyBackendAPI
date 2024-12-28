using FishyAPI.Tools;

namespace FishyAPI.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(WebApplication endpoints, SQLInteraction interactionHelper)
    {
        endpoints.MapGet("/Users/GetUsers", () =>
        {
            List<DataTypes.User> users = UserTools.GetUsers(interactionHelper);
            return users;
        }).WithName("GetUsers").WithOpenApi();

        endpoints.MapGet("/Users/GetUserById/{UID}", (ulong UID) =>
        {
            DataTypes.User user = UserTools.GetUserById(interactionHelper, UID);
            return user;
        }).WithName("GetUserById").WithOpenApi();

        endpoints.MapGet("/Users/SearchUserByName/{searchTerm}", (string searchTerm) =>
        {
            List<DataTypes.User> users = UserTools.GetUsersByName(interactionHelper, searchTerm);
            return users;
        }).WithName("SearchUserByName").WithOpenApi();

        endpoints.MapGet("/Users/GetUserByDiscordID/{DiscordID}", (ulong DiscordID) =>
        {
            DataTypes.User user = UserTools.GetUserByDiscordId(interactionHelper, DiscordID);
            return user;
        }).WithName("GetUserByDiscordID").WithOpenApi();

        endpoints.MapGet("/Users/GetUsersByRole/{Role}", (string Role) =>
        {
            List<DataTypes.User> users = UserTools.GetUsersByRole(interactionHelper, Role);
            return users;
        }).WithName("GetUsersByRole").WithOpenApi().RequireAuthorization("User");
        
        endpoints.MapPost("/Users/AddUser", (DataTypes.User user) =>
        {
            UserTools.AddUser(interactionHelper, user);
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("AddUser").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapPut("/Users/UpdateUser", (DataTypes.User user) =>
        {
            UserTools.UpdateUser(interactionHelper, user);
            return Results.Created($"/Users/GetUserById/{user.UID}", user);
        }).WithName("UpdateUser").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapDelete("/Users/DeleteUser/{UID}", (ulong UID) =>
        {
            UserTools.DeleteUser(interactionHelper, UID);
            return Results.NoContent();
        }).WithName("DeleteUser").WithOpenApi().RequireAuthorization("Organizer");
    }
}