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

        app.MapGet("/Seasons/Series/{seriesId}", (int seriesId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(SeasonTools.GetSeasonsBySeries(conn, seriesId));
        }).WithName("GetSeasonsBySeries").WithOpenApi();

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
            if (request.SeasonNumber.HasValue && request.SeasonNumber.Value < 1)
                return Results.BadRequest("SeasonNumber must be 1 or greater.");

            using MySqlConnection conn = connManager.IssueConnection();
            if (!TournamentSeriesTools.GetSeriesById(conn, request.SeriesID ?? 1).HasValue)
                return Results.BadRequest($"Series {request.SeriesID ?? 1} does not exist.");

            try
            {
                Season season = SeasonTools.CreateSeason(conn, request);
                return Results.Created($"/Seasons/{season.SeasonID}", season);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("That series already has a season with this SeasonNumber.");
            }
        }).WithName("CreateSeason").WithOpenApi().RequireAuthorization("Admin");

        app.MapPut("/Seasons/{seasonId}", (int seasonId, UpdateSeasonRequest request) =>
        {
            if (request.SeasonNumber.HasValue && request.SeasonNumber.Value < 1)
                return Results.BadRequest("SeasonNumber must be 1 or greater.");

            using MySqlConnection conn = connManager.IssueConnection();
            if (request.SeriesID.HasValue && !TournamentSeriesTools.GetSeriesById(conn, request.SeriesID.Value).HasValue)
                return Results.BadRequest($"Series {request.SeriesID.Value} does not exist.");

            try
            {
                Season? updated = SeasonTools.UpdateSeason(conn, seasonId, request);
                return updated.HasValue ? Results.Ok(updated.Value) : Results.NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("That series already has a season with this SeasonNumber.");
            }
        }).WithName("UpdateSeason").WithOpenApi().RequireAuthorization("Admin");

        app.MapDelete("/Seasons/{seasonId}", (int seasonId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            try
            {
                bool deleted = SeasonTools.DeleteSeason(conn, seasonId);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                return Results.Conflict("Season still has settings or teams attached. Remove them first.");
            }
        }).WithName("DeleteSeason").WithOpenApi().RequireAuthorization("Admin");

        // ── Season settings ──────────────────────────────────────────────────

        app.MapGet("/Seasons/{seasonId}/Settings", (int seasonId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            SeasonSettings? settings = SeasonSettingsTools.GetSettings(conn, seasonId);
            return settings.HasValue ? Results.Ok(settings.Value) : Results.NotFound();
        }).WithName("GetSeasonSettings").WithOpenApi();

        // Creates the settings row if missing; omitted fields keep their current (or default) values.
        app.MapPut("/Seasons/{seasonId}/Settings", (int seasonId, UpsertSeasonSettingsRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            if (!SeasonTools.GetSeasonById(conn, seasonId).HasValue)
                return Results.NotFound("Season not found.");

            SeasonSettings? existing = SeasonSettingsTools.GetSettings(conn, seasonId);
            var (merged, error) = SeasonSettingsTools.MergeAndValidate(existing, seasonId, request);
            if (error != null)
                return Results.BadRequest(error);

            return Results.Ok(SeasonSettingsTools.UpsertSettings(conn, merged!.Value));
        }).WithName("UpsertSeasonSettings").WithOpenApi().RequireAuthorization("Admin");

        app.MapDelete("/Seasons/{seasonId}/Settings", (int seasonId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = SeasonSettingsTools.DeleteSettings(conn, seasonId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteSeasonSettings").WithOpenApi().RequireAuthorization("Admin");
    }
}
