using FishyAPI.Tools;
namespace FishyAPI.Routes;

public static class ScoresRoutes
{
    public static void RegisterScoresRoutes(WebApplication endpoints, SQLInteraction interactionHelper)
    {
        endpoints.MapGet("/Scores/Qualifiers/{QualifierID}", (ulong QualifierID) =>
        {
            List<DataTypes.QualifierScore> scores = ScoreTools.GetQualifierScores(interactionHelper, QualifierID);
            return scores;
        }).WithName("GetQualifierScores").WithOpenApi();

        endpoints.MapGet("/Matches/GetAll", () =>
        {
            List<DataTypes.Match> matches = MatchTools.GetMatches(interactionHelper);
            return matches;
        }).WithName("GetMatches").WithOpenApi();

        endpoints.MapGet("/Matches/GetMatchById/{MID}", (ulong MID) =>
        {
            DataTypes.Match match = MatchTools.GetMatchById(interactionHelper, MID);
            return match;
        }).WithName("GetMatchById").WithOpenApi();

        endpoints.MapGet("/Matches/GetMatchesBySeason/{Season}", (int Season) =>
        {
            List<DataTypes.Match> matches = MatchTools.GetMatchesBySeason(interactionHelper, Season);
            return matches;
        }).WithName("GetMatchesBySeason").WithOpenApi();
        
        endpoints.MapPost("/Scores/AddQualifierScore", (FrontFacingDataTypes.FrontFacingQualifierScore score) =>
        {
            ScoreTools.CreateQualifierScore(interactionHelper, score);
            return Results.Created($"/Scores/Qualifiers/{score.QualifierID}", score);
        }).WithName("AddQualifierScore").WithOpenApi().RequireAuthorization(["Admin"]);
        
        endpoints.MapPost("/Scores/AddMatchScore", (FrontFacingDataTypes.FrontFacingMatchScore score) =>
        {
            ScoreTools.CreateMatchScore(interactionHelper, score);
            return Results.Created($"/Matches/GetMatchById/{score.MatchID}", score);
        }).WithName("AddMatchScore").WithOpenApi().RequireAuthorization(["Admin"]);
    }
}