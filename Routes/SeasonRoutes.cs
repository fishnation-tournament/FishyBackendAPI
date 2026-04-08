using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class SeasonRoutes
{
    public static void MapSeasonRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/Seasons", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return SeasonTools.GetSeasons(conn);
        }).WithName("GetSeasons").WithOpenApi();

        app.MapGet("/Seasons/Active", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Season? season = SeasonTools.GetActiveSeason(conn);
            return season.HasValue ? Results.Ok(season.Value) : Results.NotFound("No active season.");
        }).WithName("GetActiveSeason").WithOpenApi();

        app.MapGet("/Seasons/{seasonId}", (int seasonId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Season? season = SeasonTools.GetSeasonById(conn, seasonId);
            return season.HasValue ? Results.Ok(season.Value) : Results.NotFound();
        }).WithName("GetSeasonById").WithOpenApi();

        app.MapPost("/Seasons", (CreateSeasonRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.SeasonName))
                return Results.BadRequest("SeasonName is required.");
            if (request.EndDate <= request.StartDate)
                return Results.BadRequest("EndDate must be after StartDate.");
            if (request.QualifierCutoff.HasValue &&
                (request.QualifierCutoff.Value < request.StartDate || request.QualifierCutoff.Value > request.EndDate))
                return Results.BadRequest("QualifierCutoff must fall within the season date range.");

            using MySqlConnection conn = connManager.IssueConnection();
            Season season = SeasonTools.CreateSeason(conn, request);
            return Results.Created($"/Seasons/{season.SeasonID}", season);
        }).WithName("CreateSeason").WithOpenApi().RequireAuthorization("Admin");

        app.MapPut("/Seasons/{seasonId}", (int seasonId, UpdateSeasonRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Season? updated = SeasonTools.UpdateSeason(conn, seasonId, request);
            return updated.HasValue ? Results.Ok(updated.Value) : Results.NotFound();
        }).WithName("UpdateSeason").WithOpenApi().RequireAuthorization("Admin");

        app.MapDelete("/Seasons/{seasonId}", (int seasonId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = SeasonTools.DeleteSeason(conn, seasonId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteSeason").WithOpenApi().RequireAuthorization("Admin");
    }
}
