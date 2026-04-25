using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class FlairRoutes
{
    public static void MapFlairRoutes(WebApplication app, ConnectionManager connManager)
    {
        // --- Flair CRUD ---

        app.MapGet("/Flairs", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(FlairTools.GetAllFlairs(conn));
        }).WithName("GetAllFlairs").WithOpenApi();

        app.MapGet("/Flairs/{flairId}", (int flairId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Flair? flair = FlairTools.GetFlairById(conn, flairId);
            return flair.HasValue ? Results.Ok(flair.Value) : Results.NotFound();
        }).WithName("GetFlairById").WithOpenApi();

        app.MapPost("/Flairs", (CreateFlairRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.Color) || string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Color and Name are required.");

            using MySqlConnection conn = connManager.IssueConnection();
            Flair created = FlairTools.CreateFlair(conn, request.Color, request.Name);
            return Results.Created($"/Flairs/{created.FlairID}", created);
        }).WithName("CreateFlair").WithOpenApi().RequireAuthorization("Organizer");

        app.MapPut("/Flairs/{flairId}", (int flairId, UpdateFlairRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Flair? updated = FlairTools.UpdateFlair(conn, flairId, request.Color, request.Name);
            return updated.HasValue ? Results.Ok(updated.Value) : Results.NotFound();
        }).WithName("UpdateFlair").WithOpenApi().RequireAuthorization("Organizer");

        app.MapDelete("/Flairs/{flairId}", (int flairId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = FlairTools.DeleteFlair(conn, flairId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteFlair").WithOpenApi().RequireAuthorization("Organizer");

        // --- Map flair association ---

        app.MapPut("/Maps/{mid}/Flair/{flairId}", (ulong mid, int flairId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Flair? flair = FlairTools.GetFlairById(conn, flairId);
            if (!flair.HasValue) return Results.NotFound("Flair not found.");
            Map updated = MapTools.SetMapFlair(conn, mid, flairId);
            return Results.Ok(updated);
        }).WithName("SetMapFlair").WithOpenApi().RequireAuthorization("Organizer");

        app.MapDelete("/Maps/{mid}/Flair", (ulong mid) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Map updated = MapTools.ClearMapFlair(conn, mid);
            return Results.Ok(updated);
        }).WithName("ClearMapFlair").WithOpenApi().RequireAuthorization("Organizer");
    }
}
