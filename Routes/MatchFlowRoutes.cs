using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.MatchFlow;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

/// <summary>
/// The guarded match flow. Coordinators only choose which map or what score; the server decides whose turn it is,
/// what kind of action it is, and every result. Each action returns the new match state (also broadcast as the
/// websocket event "match_state"). Send the state's version back as expectedVersion; a stale version gets 409.
/// </summary>
public static class MatchFlowRoutes
{
    public static IResult ToResult(ActionOutcome outcome) => outcome.Status == 200
        ? Results.Ok(outcome.State)
        : Results.Json(new { error = outcome.Error, state = outcome.State }, statusCode: outcome.Status);

    public static void MapMatchFlowRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/Matches/{matchId}/State", (ulong matchId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchState? state = MatchActions.GetState(conn, matchId);
            return state != null ? Results.Ok(state) : Results.NotFound();
        }).WithName("GetMatchState").WithOpenApi();

        app.MapPost("/Matches/{matchId}/PickBan", async (ulong matchId, MatchPickBanAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "pick_ban",
                (conn, ctx, state) => MatchActions.PickBan(conn, ctx, state, request.MapID))))
            .WithName("MatchPickBan").WithOpenApi().RequirePermission(Permission.PickBansManage);

        app.MapPost("/Matches/{matchId}/Scores", async (ulong matchId, MatchScoreAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "score_submitted",
                (conn, ctx, state) => MatchActions.SubmitScore(conn, ctx, state, request.UserID, request.Score, request.MaxScore, request.Misses))))
            .WithName("MatchSubmitScore").WithOpenApi().RequirePermission(Permission.MatchScoresManage);

        app.MapPost("/Matches/{matchId}/Replay", async (ulong matchId, MatchReplayAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "replay_called",
                (conn, ctx, state) => MatchActions.CallReplay(conn, ctx, state, request.Side))))
            .WithName("MatchCallReplay").WithOpenApi().RequirePermission(Permission.MatchesUpdate);

        app.MapPost("/Matches/{matchId}/ConfirmResult", async (ulong matchId, MatchVersionedAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "result_confirmed",
                (conn, ctx, state) => MatchActions.ConfirmResult(conn, ctx, state))))
            .WithName("MatchConfirmResult").WithOpenApi().RequirePermission(Permission.MatchesUpdate);

        app.MapPost("/Matches/{matchId}/Tiebreaker", async (ulong matchId, MatchTiebreakerAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "tiebreaker_set",
                (conn, ctx, state) => MatchActions.SetTiebreaker(conn, ctx, state, request.MapID))))
            .WithName("MatchSetTiebreaker").WithOpenApi().RequirePermission(Permission.MatchesUpdate);

        app.MapPost("/Matches/{matchId}/Forfeit", async (ulong matchId, MatchForfeitAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "forfeit",
                (conn, ctx, state) => MatchActions.Forfeit(conn, ctx, state, request.Side))))
            .WithName("MatchForfeit").WithOpenApi().RequirePermission(Permission.MatchesUpdate);

        app.MapPost("/Matches/{matchId}/Undo", async (ulong matchId, MatchVersionedAction request) =>
            ToResult(await MatchActions.Run(connManager, matchId, request.ExpectedVersion, true, "undo",
                (conn, ctx, state) => MatchActions.Undo(conn, ctx, state))))
            .WithName("MatchUndo").WithOpenApi().RequirePermission(Permission.MatchesUpdate);
    }
}
