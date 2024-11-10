using MySql.Data.MySqlClient;
namespace FishyAPI.Tools;

public class MatchTools
{
    public static List<DataTypes.Match> ReadMatches(SQLInteraction interactionHelper, MySqlDataReader reader)
    {
        List<DataTypes.Match> matches = new List<DataTypes.Match>();
        while (reader.Read())
        {
            DataTypes.Match match = new DataTypes.Match();
            match.MID = reader.GetUInt64(0);
            match.MapPoolID = reader.GetUInt64(1);
            match.Player1ID = reader.GetUInt64(2);
            match.Player2ID = reader.GetUInt64(3);
            match.WinnerID = reader.GetUInt64(4);
            match.Player1Score = reader.GetInt32(5);
            match.Player2Score = reader.GetInt32(6);
            match.MatchDate = reader.GetDateTime(7);
            match.Season = reader.GetInt32(8);
            match.Scores = ScoreTools.GetMatchScores(interactionHelper, match.MID);
            matches.Add(match);
        }
        
        return matches;
    }
    
    public static DataTypes.Match ReadSingleMatch(SQLInteraction interactionHelper, MySqlDataReader reader)
    {
        DataTypes.Match match = new DataTypes.Match();
        match.MID = reader.GetUInt64(0);
        match.MapPoolID = reader.GetUInt64(1);
        match.Player1ID = reader.GetUInt64(2);
        match.Player2ID = reader.GetUInt64(3);
        match.WinnerID = reader.GetUInt64(4);
        match.Player1Score = reader.GetInt32(5);
        match.Player2Score = reader.GetInt32(6);
        match.MatchDate = reader.GetDateTime(7);
        match.Season = reader.GetInt32(8);
        match.Scores = ScoreTools.GetMatchScores(interactionHelper, match.MID);
        return match;
    }
    
    public static List<DataTypes.Match> GetMatches(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM matches";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.Match> matches = ReadMatches(interactionHelper, reader);
        reader.Close();
        return matches;
    }
    
    public static List<DataTypes.Match> GetMatchesBySeason(SQLInteraction interactionHelper, int season)
    {
        string query = $"SELECT * FROM matches WHERE Season = {season}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.Match> matches = ReadMatches(interactionHelper, reader);
        reader.Close();
        return matches;
    }
}