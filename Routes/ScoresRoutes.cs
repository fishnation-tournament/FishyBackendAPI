using FishyAPI.Tools;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

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

        endpoints.MapPost("/Scores/AddMatchScore", (FrontFacingMatchScore score) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            ScoreTools.CreateMatchScore(conn, score);
            return Results.Created($"/Matches/GetMatchById/{score.MatchID}", score);
        }).WithName("AddMatchScore").WithOpenApi().RequireAuthorization("Admin");

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

        endpoints.MapGet("/Matches/GetMatchesBySeason/{Season}", (int Season) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Match> matches = MatchTools.GetMatchesBySeason(conn, Season);
            return matches;
        }).WithName("GetMatchesBySeason").WithOpenApi();

        endpoints.MapPost("/Matches/Create", (FrontFacingMatch match) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Match created = MatchTools.CreateMatch(conn, match);
            return Results.Created($"/Matches/GetMatchById/{created.MatchID}", created);
        }).WithName("CreateMatch").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Matches/Update", (Match match) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchTools.UpdateMatch(conn, match);
            Match updated = MatchTools.GetMatchById(conn, match.MatchID);
            return Results.Ok(updated);
        }).WithName("UpdateMatch").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Matches/Delete/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchTools.DeleteMatch(conn, MID);
            return Results.NoContent();
        }).WithName("DeleteMatch").WithOpenApi().RequireAuthorization("Organizer");
    }
}
