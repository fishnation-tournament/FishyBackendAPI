using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class PlayerRoutes
{
    public static void MapPlayerRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Players/Register", (HttpContext context) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (uidClaim == null)
            {
                return Results.Unauthorized();
            }

            ulong uid = ulong.Parse(uidClaim.Value);
            
            if(PlayerTools.IsPlayerInCurrentSeason(conn, uid))
            {
                return Results.BadRequest("Player is already registered for the current season.");
            }
            
            PlayerTools.AddPlayer(conn, new Player
            {
                PlayerID = null,
                UID = uid,
                Season = 2,
                Seed = null
            });
            
            return Results.Ok();
        }).WithName("RegisterPlayer").WithOpenApi().RequireAuthorization("User");
        endpoints.MapGet("/Players/GetPlayers", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Player> players = PlayerTools.GetPlayers(conn);
            return players;
        }).WithName("GetPlayers").WithOpenApi();

        endpoints.MapGet("/Players/GetPlayerByID/{id}", (int id) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Player player = PlayerTools.GetPlayerByID(conn, id);
            return player;
        }).WithName("GetPlayerByID").WithOpenApi();

        endpoints.MapGet("/Players/SearchPlayerByName/{searchTerm}", (string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Player> players = PlayerTools.GetPlayersByName(conn, searchTerm);
            return players;
        }).WithName("SearchPlayerByName").WithOpenApi();

        endpoints.MapGet("/Players/GetPlayersBySeason/{season}", (int season) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Player> players = PlayerTools.GetPlayersBySeason(conn, season);
            return players;
        }).WithName("GetPlayersBySeason").WithOpenApi();

        endpoints.MapGet("/Players/GetPlayerByUID/{uid}", (ulong uid) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Player> players = PlayerTools.GetPlayerByUID(conn, new List<ulong> { uid });
            return players;
        }).WithName("GetPlayerByUID").WithOpenApi();

        endpoints.MapPost("/Players/GetPlayersByUID", (List<ulong> ssids) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Player> players = PlayerTools.GetPlayerByUID(conn, ssids);
            return players;
        }).WithName("GetPlayersByUID").WithOpenApi();

        endpoints.MapPost("/Players/AddPlayer", (Player player) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Player newPlayer = PlayerTools.AddPlayer(conn, player);
            return Results.Created($"/Players/GetPlayerByID/{newPlayer.PlayerID}", newPlayer);
        }).WithName("AddPlayer").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Players/UpdatePlayer", (Player player) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            PlayerTools.UpdatePlayer(conn, player);
            return Results.Created($"/Players/GetPlayerByID/{player.PlayerID}", player);
        }).WithName("UpdatePlayer").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Players/DeletePlayer/{playerID}", (int playerID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            PlayerTools.DeletePlayer(conn, playerID);
            return Results.NoContent();
        }).WithName("DeletePlayer").WithOpenApi().RequireAuthorization("Organizer");
    }
}

