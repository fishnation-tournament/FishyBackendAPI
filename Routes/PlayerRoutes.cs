using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Tools.FrontFacingDataTypes;
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
                return Results.Unauthorized();

            ulong uid = ulong.Parse(uidClaim.Value);

            User user = UserTools.GetUserById(conn, uid);
            if (!user.SSID.HasValue)
                return Results.BadRequest("A ScoreSaber ID must be linked to your account before registering as a player.");

            Season? activeSeason = SeasonTools.GetActiveSeason(conn);
            if (!activeSeason.HasValue)
                return Results.BadRequest("There is no active season to register for.");

            if (PlayerTools.IsPlayerInCurrentSeason(conn, uid))
            {
                Console.WriteLine($"Player with UID {uid} is already registered for season {activeSeason.Value.SeasonID}.");
                return Results.BadRequest("Player is already registered for the current season.");
            }

            Console.WriteLine($"Player with UID {uid} is being registered for season {activeSeason.Value.SeasonID}.");

            Player returnPlayer = PlayerTools.AddPlayer(conn, new Player
            {
                PlayerID = null,
                UID = uid,
                Season = activeSeason.Value.SeasonID,
                Seed = null
            });

            return Results.Created($"/Players/GetPlayerByID/{returnPlayer.PlayerID}", returnPlayer);
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

        endpoints.MapPost("/Players/AddPlayer", (AddPlayer addPlayer) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Player player = new Player
            {
                PlayerID = null,
                UID = addPlayer.UID,
                Season = addPlayer.Season,
                Seed = addPlayer.Seed
            };
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

