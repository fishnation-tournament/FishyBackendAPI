using System.Text.RegularExpressions;
using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class TournamentSeriesRoutes
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    private static string? ValidateSlug(string? slug)
    {
        if (slug == null) return null;
        if (slug.Length is < 1 or > 50 || !SlugPattern.IsMatch(slug))
            return "Slug must be 1-50 lowercase letters, digits, or single hyphens (e.g. 'fish-nation').";
        return null;
    }

    public static void MapTournamentSeriesRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/Series", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(TournamentSeriesTools.GetAllSeries(conn));
        }).WithName("GetAllSeries").WithOpenApi();

        app.MapGet("/Series/{seriesId}", (int seriesId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            TournamentSeries? series = TournamentSeriesTools.GetSeriesById(conn, seriesId);
            return series.HasValue ? Results.Ok(series.Value) : Results.NotFound();
        }).WithName("GetSeriesById").WithOpenApi();

        app.MapGet("/Series/Slug/{slug}", (string slug) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            TournamentSeries? series = TournamentSeriesTools.GetSeriesBySlug(conn, slug);
            return series.HasValue ? Results.Ok(series.Value) : Results.NotFound();
        }).WithName("GetSeriesBySlug").WithOpenApi();

        app.MapPost("/Series", (CreateTournamentSeriesRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.SeriesName))
                return Results.BadRequest("SeriesName is required.");
            if (request.Slug == null)
                return Results.BadRequest("Slug is required.");
            string? slugError = ValidateSlug(request.Slug);
            if (slugError != null)
                return Results.BadRequest(slugError);

            using MySqlConnection conn = connManager.IssueConnection();
            try
            {
                TournamentSeries created = TournamentSeriesTools.CreateSeries(conn, request);
                return Results.Created($"/Series/{created.SeriesID}", created);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("A series with that slug already exists.");
            }
        }).WithName("CreateSeries").WithOpenApi().RequireAuthorization("Admin");

        app.MapPut("/Series/{seriesId}", (int seriesId, UpdateTournamentSeriesRequest request) =>
        {
            if (request.SeriesName != null && string.IsNullOrWhiteSpace(request.SeriesName))
                return Results.BadRequest("SeriesName cannot be blank.");
            string? slugError = ValidateSlug(request.Slug);
            if (slugError != null)
                return Results.BadRequest(slugError);

            using MySqlConnection conn = connManager.IssueConnection();
            try
            {
                TournamentSeries? updated = TournamentSeriesTools.UpdateSeries(conn, seriesId, request);
                return updated.HasValue ? Results.Ok(updated.Value) : Results.NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict("A series with that slug already exists.");
            }
        }).WithName("UpdateSeries").WithOpenApi().RequireAuthorization("Admin");

        app.MapDelete("/Series/{seriesId}", (int seriesId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            try
            {
                bool deleted = TournamentSeriesTools.DeleteSeries(conn, seriesId);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (MySqlException ex) when (ex.Number == 1451)
            {
                return Results.Conflict("Series still has seasons attached. Move or delete them first.");
            }
        }).WithName("DeleteSeries").WithOpenApi().RequireAuthorization("Admin");
    }
}
