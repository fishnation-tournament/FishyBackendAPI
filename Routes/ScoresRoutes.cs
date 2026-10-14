using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Tools.MatchFlow;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace FishyAPI.Routes;

public static class ScoresRoutes
{
    public static void RegisterScoresRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        // ── Qualifier Scores ────────────────────────────────────────────────

        endpoints.MapGet("/Scores/Qualifiers/{QualifierID}", (ulong QualifierID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<QualifierScore> scores = ScoreTools.GetQualifierScores(conn, QualifierID);
            return scores;
        }).WithName("GetQualifierScores").WithOpenApi();

        endpoints.MapGet("/Scores/Qualifiers/{QualifierID}/Map/{MapID}", (ulong QualifierID, ulong MapID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<QualifierScore> scores = ScoreTools.GetQualifierScoreByMapID(conn, QualifierID, MapID);
            return scores;
        }).WithName("GetQualifierScoresByMap").WithOpenApi();

        endpoints.MapGet("/Scores/Qualifiers/{QualifierID}/Scoresaber/{ScoresaberID}", (ulong QualifierID, ulong ScoresaberID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<QualifierScore> scores = ScoreTools.GetQualifierScoresByScoresaberID(conn, QualifierID, ScoresaberID);
            return scores;
        }).WithName("GetQualifierScoresByScoresaberID").WithOpenApi();

        endpoints.MapGet("/Scores/Qualifiers/Scoresaber/{ScoresaberID}/Map/{MapID}", (ulong ScoresaberID, ulong MapID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            QualifierScore score = ScoreTools.GetQualifierScoreByScoresaberIDAndMapID(conn, ScoresaberID, MapID);
            return score;
        }).WithName("GetQualifierScoreByScoresaberIDAndMapID").WithOpenApi();

        endpoints.MapPost("/Scores/AddQualifierScore", (AddQualifierScore score) =>
        {
            Console.WriteLine($"AddQualifierScore hit: ScoresaberID={score.ScoresaberID}, QualifierID={score.QualifierID}, MapID={score.MapID}, Score={score.Score}");
            using MySqlConnection conn = connManager.IssueConnection();

            Season? activeSeason = SeasonTools.GetActiveSeason(conn);
            if (!activeSeason.HasValue)
            {
                Console.WriteLine(
                    $"Attempt to submit qualifier score for ScoresaberID {score.ScoresaberID} when there is no active season.");
                return Results.BadRequest("No active season.");
            }

            if (!activeSeason.Value.QualifiersOpen)
            {
                Console.WriteLine($"Attempt to submit qualifier score for season {activeSeason.Value.SeasonID} when qualifiers are closed.");
                return Results.BadRequest("Qualifier submissions are closed for this season.");
            }

            FrontFacingQualifierScore qualifierScore = new FrontFacingQualifierScore(score);
            ScoreTools.CreateQualifierScore(conn, qualifierScore);
            return Results.Created($"/Scores/Qualifiers/{score.QualifierID}", qualifierScore);
        }).WithName("AddQualifierScore").WithOpenApi().RequirePermission(Permission.QualifierScoresManage);

        endpoints.MapDelete("/Scores/Qualifier/{scoreId}", (ulong scoreId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = ScoreTools.DeleteQualifierScore(conn, scoreId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteQualifierScore").WithOpenApi().RequirePermission(Permission.QualifierScoresManage);

        // ── Match Scores ─────────────────────────────────────────────────────

        endpoints.MapGet("/Scores/Match/{MatchID}", (ulong MatchID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<MatchScore> scores = ScoreTools.GetMatchScores(conn, MatchID);
            return scores;
        }).WithName("GetMatchScores").WithOpenApi();

        // Overrides: edit a match's score records directly (e.g. to fix a past map). Coordinators normally use
        // POST /Matches/{id}/Scores. Points, winner and replay flags are recomputed from the records afterwards.

        endpoints.MapPost("/Scores/AddMatchScore", async (FrontFacingMatchScore score) =>
            MatchFlowRoutes.ToResult(await MatchActions.Run(connManager, score.MatchID, null, false, "score_override",
                (conn, ctx, state) => MatchActions.OverrideAddScore(conn, ctx, state, score))))
            .WithName("AddMatchScore").WithOpenApi().RequirePermission(Permission.MatchesOverride);

        endpoints.MapPut("/Scores/Match/{scoreId}", async (ulong scoreId, UpdateMatchScoreRequest request) =>
        {
            MatchScore? existing;
            using (MySqlConnection conn = connManager.IssueConnection())
                existing = ScoreTools.GetMatchScoreById(conn, scoreId);
            if (existing == null) return Results.NotFound();
            return MatchFlowRoutes.ToResult(await MatchActions.Run(connManager, existing.Value.MatchID, null, false, "score_override",
                (conn, ctx, state) => MatchActions.OverrideUpdateScore(conn, ctx, scoreId, request)));
        }).WithName("UpdateMatchScore").WithOpenApi().RequirePermission(Permission.MatchesOverride);

        endpoints.MapDelete("/Scores/Match/{scoreId}", async (ulong scoreId) =>
        {
            MatchScore? existing;
            using (MySqlConnection conn = connManager.IssueConnection())
                existing = ScoreTools.GetMatchScoreById(conn, scoreId);
            if (existing == null) return Results.NotFound();
            return MatchFlowRoutes.ToResult(await MatchActions.Run(connManager, existing.Value.MatchID, null, false, "score_override",
                (conn, ctx, state) => MatchActions.OverrideDeleteScore(conn, ctx, scoreId)));
        }).WithName("DeleteMatchScore").WithOpenApi().RequirePermission(Permission.MatchScoresDelete);

        // ── Matches ───────────────────────────────────────────────────────────

        endpoints.MapGet("/Matches/GetAll", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Match> matches = MatchTools.GetMatches(conn);
            return matches;
        }).WithName("GetMatches").WithOpenApi();

        endpoints.MapGet("/Matches/GetMatchById/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Match match = MatchTools.GetMatchById(conn, MID);
            return match;
        }).WithName("GetMatchById").WithOpenApi();

        endpoints.MapGet("/Matches/GetMatchesBySeason/{Season}", (int Season, bool sortByRound = false) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Match> matches = MatchTools.GetMatchesBySeason(conn, Season, sortByRound);
            return matches;
        }).WithName("GetMatchesBySeason").WithOpenApi();

        endpoints.MapPost("/Matches/Create", (FrontFacingMatch match) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Match created = MatchTools.CreateMatch(conn, match);
            return Results.Created($"/Matches/GetMatchById/{created.MatchID}", created);
        }).WithName("CreateMatch").WithOpenApi().RequirePermission(Permission.MatchesManage);

        endpoints.MapPost("/Matches/BulkCreate", (List<BulkCreateMatchEntry> entries) =>
        {
            if (entries.Count == 0)
                return Results.BadRequest("At least one match entry is required.");

            using MySqlConnection conn = connManager.IssueConnection();
            List<BulkMatchCreationResult> results = MatchTools.BulkCreateMatches(conn, entries);
            return Results.Ok(results);
        }).WithName("BulkCreateMatches").WithOpenApi().RequirePermission(Permission.MatchesManage);

        // Edits match details only. Result fields in the body (points, winner, complete, replays, status) are
        // ignored: results come from the match's records via the guarded actions.
        endpoints.MapPut("/Matches/Update", async (Match match) =>
        {
            ActionOutcome outcome = await MatchActions.Run(connManager, match.MatchID, null, false, "match_details_updated",
                (conn, ctx, state) => MatchActions.UpdateMetadata(conn, ctx, state, match));
            if (outcome.Status != 200) return MatchFlowRoutes.ToResult(outcome);
            using MySqlConnection conn = connManager.IssueConnection();
            return Results.Ok(MatchTools.GetMatchById(conn, match.MatchID));
        }).WithName("UpdateMatch").WithOpenApi().RequirePermission(Permission.MatchesUpdate);

        endpoints.MapDelete("/Matches/Delete/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchTools.DeleteMatch(conn, MID);
            return Results.NoContent();
        }).WithName("DeleteMatch").WithOpenApi().RequirePermission(Permission.MatchesManage);

        // Legacy: same rules as POST /Matches/{id}/Replay, identifying the caller by ScoreSaber ID.
        endpoints.MapPost("/Matches/{matchId}/CallReplay", async (ulong matchId, CallReplayRequest request) =>
            MatchFlowRoutes.ToResult(await MatchActions.Run(connManager, matchId, null, false, "replay_called",
                (conn, ctx, state) =>
                {
                    Participant? caller = ctx.Participants.FirstOrDefault(p => p.Ssid == request.CallingPlayerUID);
                    return caller == null
                        ? new ActionError(400, "The calling player isn't in this match.")
                        : MatchActions.CallReplay(conn, ctx, state, caller.Side);
                })))
            .WithName("CallReplay").WithOpenApi().RequirePermission(Permission.MatchesUpdate);
    }
}
