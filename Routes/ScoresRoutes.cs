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
        endpoints.MapGet("/Scores/Qualifiers/{QualifierID}", (ulong QualifierID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<QualifierScore> scores = ScoreTools.GetQualifierScores(conn, QualifierID);
            return scores;
        }).WithName("GetQualifierScores").WithOpenApi();

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

        endpoints.MapPost("/Scores/AddQualifierScore", (FrontFacingQualifierScore score) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            ScoreTools.CreateQualifierScore(conn, score);
            return Results.Created($"/Scores/Qualifiers/{score.QualifierID}", score);
        }).WithName("AddQualifierScore").WithOpenApi().RequireAuthorization(["Admin"]);

        endpoints.MapPost("/Scores/AddMatchScore", (FrontFacingMatchScore score) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            ScoreTools.CreateMatchScore(conn, score);
            return Results.Created($"/Matches/GetMatchById/{score.MatchID}", score);
        }).WithName("AddMatchScore").WithOpenApi().RequireAuthorization(["Admin"]);
    }
}
