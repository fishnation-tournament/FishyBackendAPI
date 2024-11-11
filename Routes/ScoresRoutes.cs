using FishyAPI.Tools;
namespace FishyAPI.Routes;

public class ScoresRoutes
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

    }
}