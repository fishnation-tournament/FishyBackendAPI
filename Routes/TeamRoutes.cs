using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class TeamRoutes
{
    public static void MapTeamRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/Teams/Season/{seasonId}", (int seasonId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(TeamTools.GetTeamsBySeason(conn, seasonId));
        }).WithName("GetTeamsBySeason").WithOpenApi();

        app.MapGet("/Teams/User/{userId}", (ulong userId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(TeamTools.GetTeamsByUser(conn, userId));
        }).WithName("GetTeamsByUser").WithOpenApi();

        app.MapGet("/Teams/{teamId}", (int teamId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Team? team = TeamTools.GetTeamById(conn, teamId);
            return team.HasValue ? Results.Ok(team.Value) : Results.NotFound();
        }).WithName("GetTeamById").WithOpenApi();

        app.MapPost("/Teams", (CreateTeamRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.TeamName) || request.TeamName.Length > 100)
                return Results.BadRequest("TeamName is required and must be at most 100 characters.");

            using MySqlConnection conn = connManager.IssueConnection();
            if (!SeasonTools.GetSeasonById(conn, request.SeasonID).HasValue)
                return Results.BadRequest("Season not found.");
            if (!SeasonSettingsTools.IsTeamSeason(conn, request.SeasonID))
                return Results.BadRequest($"Season {request.SeasonID} is not a team season. Set ParticipantType to 'team' via PUT /Seasons/{request.SeasonID}/Settings first.");
            if (request.CaptainUID.HasValue && UserTools.GetUserById(conn, request.CaptainUID.Value).UID == 0)
                return Results.BadRequest("Captain user not found.");

            try
            {
                Team created = TeamTools.CreateTeam(conn, request);
                return Results.Created($"/Teams/{created.TeamID}", created);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("Team name is already taken this season, or the captain is already on a team this season.");
            }
        }).WithName("CreateTeam").WithOpenApi().RequireAuthorization("Organizer");

        app.MapPut("/Teams/{teamId}", (int teamId, UpdateTeamRequest request) =>
        {
            if (request.TeamName != null && (string.IsNullOrWhiteSpace(request.TeamName) || request.TeamName.Length > 100))
                return Results.BadRequest("TeamName must be 1-100 characters.");

            using MySqlConnection conn = connManager.IssueConnection();
            if (request.CaptainUID.HasValue && !request.ClearCaptain &&
                !TeamTools.IsTeamMember(conn, teamId, request.CaptainUID.Value))
                return Results.BadRequest("Captain must be a member of the team.");

            try
            {
                Team? updated = TeamTools.UpdateTeam(conn, teamId, request);
                return updated.HasValue ? Results.Ok(updated.Value) : Results.NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("Team name is already taken this season.");
            }
        }).WithName("UpdateTeam").WithOpenApi().RequireAuthorization("Organizer");

        app.MapDelete("/Teams/{teamId}", (int teamId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            try
            {
                bool deleted = TeamTools.DeleteTeam(conn, teamId);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                return Results.Conflict("Team has lineups, pick/bans, or scores recorded and cannot be deleted.");
            }
        }).WithName("DeleteTeam").WithOpenApi().RequireAuthorization("Organizer");

        // ── Members ──────────────────────────────────────────────────────────

        app.MapPost("/Teams/{teamId}/Members", (int teamId, AddTeamMemberRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Team? team = TeamTools.GetTeamById(conn, teamId);
            if (!team.HasValue) return Results.NotFound("Team not found.");
            if (UserTools.GetUserById(conn, request.UserID).UID == 0)
                return Results.BadRequest("User not found.");

            SeasonSettings? settings = SeasonSettingsTools.GetSettings(conn, team.Value.SeasonID);
            if (settings?.TeamMaxSize is int maxSize && team.Value.Members.Count >= maxSize)
                return Results.BadRequest($"Team is full ({maxSize} members max).");

            try
            {
                TeamTools.AddTeamMember(conn, team.Value, request.UserID);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("User is already on a team this season.");
            }

            return Results.Created($"/Teams/{teamId}", TeamTools.GetTeamById(conn, teamId)!.Value);
        }).WithName("AddTeamMember").WithOpenApi().RequireAuthorization("Organizer");

        app.MapDelete("/Teams/{teamId}/Members/{userId}", (int teamId, ulong userId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool removed = TeamTools.RemoveTeamMember(conn, teamId, userId);
            return removed ? Results.NoContent() : Results.NotFound();
        }).WithName("RemoveTeamMember").WithOpenApi().RequireAuthorization("Organizer");
    }
}
