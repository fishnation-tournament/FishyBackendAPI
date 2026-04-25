using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class PickBanRoutes
{
    public static void MapPickBanRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/PickBan/Match/{matchId}", (ulong matchId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<PickBan> entries = PickBanTools.GetPickBansByMatch(conn, matchId);
            return Results.Ok(entries);
        }).WithName("GetPickBansByMatch").WithOpenApi();

        app.MapGet("/PickBan/{pickBanId}", (int pickBanId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            PickBan? entry = PickBanTools.GetPickBanById(conn, pickBanId);
            return entry.HasValue ? Results.Ok(entry.Value) : Results.NotFound();
        }).WithName("GetPickBanById").WithOpenApi();

        app.MapPost("/PickBan", async (CreatePickBanRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            PickBan created = PickBanTools.CreatePickBan(conn, request);

            string eventType = created.IsPick ? "pick_added" : "ban_added";
            await MatchWebSocketManager.BroadcastToMatch(created.MatchID, eventType, created);

            return Results.Created($"/PickBan/{created.PickBanID}", created);
        }).WithName("CreatePickBan").WithOpenApi().RequireAuthorization("Organizer");

        app.MapDelete("/PickBan/{pickBanId}", async (int pickBanId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            PickBan? entry = PickBanTools.GetPickBanById(conn, pickBanId);
            if (!entry.HasValue) return Results.NotFound();

            bool deleted = PickBanTools.DeletePickBan(conn, pickBanId);
            if (deleted)
                await MatchWebSocketManager.BroadcastToMatch(entry.Value.MatchID, "pick_ban_removed", new { pickBanId });

            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeletePickBan").WithOpenApi().RequireAuthorization("Organizer");
    }
}
