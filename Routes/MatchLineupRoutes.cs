using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class MatchLineupRoutes
{
    public static void MapMatchLineupRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/Lineups/Match/{matchId}", (ulong matchId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(MatchLineupTools.GetLineupsByMatch(conn, matchId));
        }).WithName("GetLineupsByMatch").WithOpenApi();

        app.MapGet("/Lineups/{lineupId}", (ulong lineupId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchLineup? lineup = MatchLineupTools.GetLineupById(conn, lineupId);
            return lineup.HasValue ? Results.Ok(lineup.Value) : Results.NotFound();
        }).WithName("GetLineupById").WithOpenApi();

        app.MapPost("/Lineups", async (CreateMatchLineupRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();

            Match match = MatchTools.GetMatchById(conn, request.MatchID);
            if (match.MatchID == 0)
                return Results.BadRequest("Match not found.");
            if (match.Team1ID != request.TeamID && match.Team2ID != request.TeamID)
                return Results.BadRequest("Team is not playing in this match.");
            if (MapTools.GetMapById(conn, request.MapID).MID == 0)
                return Results.BadRequest("Map not found.");
            if (!TeamTools.IsTeamMember(conn, request.TeamID, request.UserID))
                return Results.BadRequest("User is not a member of that team.");

            SeasonSettings? settings = SeasonSettingsTools.GetSettings(conn, match.Season);
            if (settings?.TeamActivePlayers is int activePlayers &&
                MatchLineupTools.CountLineupEntries(conn, request.MatchID, request.MapID, request.TeamID) >= activePlayers)
                return Results.BadRequest($"Lineup for this map is full ({activePlayers} active players).");

            MatchLineup created;
            try
            {
                created = MatchLineupTools.CreateLineup(conn, request);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("User is already in the lineup for this map.");
            }

            await MatchWebSocketManager.BroadcastToMatch(created.MatchID, "lineup_added", created);
            return Results.Created($"/Lineups/{created.LineupID}", created);
        }).WithName("CreateLineup").WithOpenApi().RequireAuthorization("Organizer");

        app.MapDelete("/Lineups/{lineupId}", async (ulong lineupId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchLineup? lineup = MatchLineupTools.GetLineupById(conn, lineupId);
            if (!lineup.HasValue) return Results.NotFound();

            bool deleted = MatchLineupTools.DeleteLineup(conn, lineupId);
            if (deleted)
                await MatchWebSocketManager.BroadcastToMatch(lineup.Value.MatchID, "lineup_removed", new { lineupId });

            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteLineup").WithOpenApi().RequireAuthorization("Organizer");
    }
}
