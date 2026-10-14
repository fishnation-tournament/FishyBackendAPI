using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Tools.MatchFlow;
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

        // Overrides: add or remove a pick/ban record out of turn (e.g. to fix a mistake). Coordinators normally use
        // POST /Matches/{id}/PickBan. The match's results are recomputed from the records afterwards.

        app.MapPost("/PickBan", async (CreatePickBanRequest request) =>
        {
            if (!request.PlayerID.HasValue && !request.TeamID.HasValue)
                return Results.BadRequest("Either PlayerID or TeamID is required.");

            return MatchFlowRoutes.ToResult(await MatchActions.Run(connManager, request.MatchID, null, false, "pick_ban_override",
                (conn, ctx, state) => MatchActions.OverrideAddPickBan(conn, ctx, state, request)));
        }).WithName("CreatePickBan").WithOpenApi().RequirePermission(Permission.MatchesOverride);

        app.MapDelete("/PickBan/{pickBanId}", async (int pickBanId) =>
        {
            PickBan? entry;
            using (MySqlConnection conn = connManager.IssueConnection())
                entry = PickBanTools.GetPickBanById(conn, pickBanId);
            if (!entry.HasValue) return Results.NotFound();

            return MatchFlowRoutes.ToResult(await MatchActions.Run(connManager, entry.Value.MatchID, null, false, "pick_ban_override",
                (conn, ctx, state) => MatchActions.OverrideDeletePickBan(conn, ctx, pickBanId)));
        }).WithName("DeletePickBan").WithOpenApi().RequirePermission(Permission.MatchesOverride);
    }
}
