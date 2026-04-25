using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
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
        }).WithName("AddQualifierScore").WithOpenApi().RequireAuthorization("Admin");

        endpoints.MapDelete("/Scores/Qualifier/{scoreId}", (ulong scoreId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = ScoreTools.DeleteQualifierScore(conn, scoreId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteQualifierScore").WithOpenApi().RequireAuthorization("Admin");

        // ── Match Scores ─────────────────────────────────────────────────────

        endpoints.MapGet("/Scores/Match/{MatchID}", (ulong MatchID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<MatchScore> scores = ScoreTools.GetMatchScores(conn, MatchID);
            return scores;
        }).WithName("GetMatchScores").WithOpenApi();

        endpoints.MapPost("/Scores/AddMatchScore", async (FrontFacingMatchScore score) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchScore? completeScore = ScoreTools.CreateMatchScore(conn, score);
            if (completeScore != null)
            {
                await MatchWebSocketManager.BroadcastToMatch(score.MatchID, "score_posted", completeScore);
            }

            return Results.Created($"/Matches/GetMatchById/{score.MatchID}", score);
        }).WithName("AddMatchScore").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Scores/Match/{scoreId}", async (ulong scoreId, UpdateMatchScoreRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchScore? updated = ScoreTools.UpdateMatchScore(conn, scoreId, request);
            if (updated == null) return Results.NotFound();
            await MatchWebSocketManager.BroadcastToMatch(updated.Value.MatchID, "score_updated", updated.Value);
            return Results.Ok(updated.Value);
        }).WithName("UpdateMatchScore").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Scores/Match/{scoreId}", (ulong scoreId) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = ScoreTools.DeleteMatchScore(conn, scoreId);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteMatchScore").WithOpenApi().RequireAuthorization("Admin");

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
        }).WithName("CreateMatch").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPost("/Matches/BulkCreate", (List<BulkCreateMatchEntry> entries) =>
        {
            if (entries.Count == 0)
                return Results.BadRequest("At least one match entry is required.");

            using MySqlConnection conn = connManager.IssueConnection();
            List<BulkMatchCreationResult> results = MatchTools.BulkCreateMatches(conn, entries);
            return Results.Ok(results);
        }).WithName("BulkCreateMatches").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Matches/Update", async (Match match) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchTools.UpdateMatch(conn, match);
            Match updated = MatchTools.GetMatchById(conn, match.MatchID);
            if (updated.Complete)
                MatchTools.AdvancePlayersFromMatch(conn, updated);
            await MatchWebSocketManager.BroadcastToMatch(updated.MatchID, "match_updated", updated);
            return Results.Ok(updated);
        }).WithName("UpdateMatch").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Matches/Delete/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchTools.DeleteMatch(conn, MID);
            return Results.NoContent();
        }).WithName("DeleteMatch").WithOpenApi().RequireAuthorization("Organizer");

        // A player may call a replay once per match when they lose a point.
        // Voids the scores for the most recently played map and marks the player's replay as spent.
        endpoints.MapPost("/Matches/{matchId}/CallReplay", async (ulong matchId, CallReplayRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();

            Match? updatedMatch = MatchTools.SetPlayerReplayUsed(conn, matchId, request.CallingPlayerUID);
            if (updatedMatch == null)
                return Results.BadRequest("Replay not available: match not found, player not in match, or replay already used.");

            List<MatchScore> preReplayScores = ScoreTools.FlagPreReplayScores(conn, matchId);

            await MatchWebSocketManager.BroadcastToMatch(matchId, "replay_called", new
            {
                callingPlayerUID = request.CallingPlayerUID,
                preReplayScores,
                match = updatedMatch.Value
            });

            return Results.Ok(new { match = updatedMatch.Value, preReplayScores });
        }).WithName("CallReplay").WithOpenApi().RequireAuthorization("Organizer");
    }
}
