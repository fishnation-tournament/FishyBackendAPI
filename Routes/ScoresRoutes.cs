using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;

namespace FishyAPI.Routes;

public static class ScoresRoutes
{
    public static void RegisterScoresRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Scores/Qualifiers/{QualifierID}", (ulong QualifierID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.QualifierScore> scores = ScoreTools.GetQualifierScores(conn.Connection, QualifierID);
            conn.Close();
            return scores;
        }).WithName("GetQualifierScores").WithOpenApi();

        endpoints.MapGet("/Matches/GetAll", () =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.Match> matches = MatchTools.GetMatches(conn.Connection);
            conn.Close();
            return matches;
        }).WithName("GetMatches").WithOpenApi();

        endpoints.MapGet("/Matches/GetMatchById/{MID}", (ulong MID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            DataTypes.Match match = MatchTools.GetMatchById(conn.Connection, MID);
            conn.Close();
            return match;
        }).WithName("GetMatchById").WithOpenApi();

        endpoints.MapGet("/Matches/GetMatchesBySeason/{Season}", (int Season) =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.Match> matches = MatchTools.GetMatchesBySeason(conn.Connection, Season);
            conn.Close();
            return matches;
        }).WithName("GetMatchesBySeason").WithOpenApi();

        endpoints.MapPost("/Scores/AddQualifierScore", (FrontFacingDataTypes.FrontFacingQualifierScore score) =>
        {
            DBConnection conn = connManager.IssueConnection();
            ScoreTools.CreateQualifierScore(conn.Connection, score);
            conn.Close();
            return Results.Created($"/Scores/Qualifiers/{score.QualifierID}", score);
        }).WithName("AddQualifierScore").WithOpenApi().RequireAuthorization(["Admin"]);

        endpoints.MapPost("/Scores/AddMatchScore", (FrontFacingDataTypes.FrontFacingMatchScore score) =>
        {
            DBConnection conn = connManager.IssueConnection();
            ScoreTools.CreateMatchScore(conn.Connection, score);
            conn.Close();
            return Results.Created($"/Matches/GetMatchById/{score.MatchID}", score);
        }).WithName("AddMatchScore").WithOpenApi().RequireAuthorization(["Admin"]);
    }
}