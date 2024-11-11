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
            match.Player1ID = reader.GetUInt64(1);
            match.Player2ID = reader.GetUInt64(2);
            match.Player1Score = reader.GetInt32(3);
            match.Player2Score = reader.GetInt32(4);
            match.MatchDate = reader.GetDateTime(5);
            match.Complete = reader.GetBoolean(6);
            match.WinnerID = reader.GetUInt64(7);
            match.Season = reader.GetInt32(8);
            match.MapPoolID = reader.GetUInt64(9);
            match.Scores = new List<DataTypes.MatchScore>();
            matches.Add(match);
        }
        reader.Close();
        
        for(int i = 0; i < matches.Count; i++)
        {
            var tempMatch = matches[i];
            tempMatch.Scores = ScoreTools.GetMatchScores(interactionHelper, tempMatch.MID);
            matches[i] = tempMatch;
        }
        return matches;
    }
    
    public static DataTypes.Match ReadSingleMatch(SQLInteraction interactionHelper, MySqlDataReader reader)
    {
        DataTypes.Match match = new DataTypes.Match();
        
        while (reader.Read())
        {
            match.MID = reader.GetUInt64(0);
            match.Player1ID = reader.GetUInt64(1);
            match.Player2ID = reader.GetUInt64(2);
            match.Player1Score = reader.GetInt32(3);
            match.Player2Score = reader.GetInt32(4);
            match.MatchDate = reader.GetDateTime(5);
            match.Complete = reader.GetBoolean(6);
            match.WinnerID = reader.GetUInt64(7);
            match.Season = reader.GetInt32(8);
            match.MapPoolID = reader.GetUInt64(9);
        }
        reader.Close();
        
        match.Scores = ScoreTools.GetMatchScores(interactionHelper, match.MID);
        return match;
    }
    
    public static List<DataTypes.Match> GetMatches(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM matches";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.Match> matches = ReadMatches(interactionHelper, reader);

        return matches;
    }
    
    public static List<DataTypes.Match> GetMatchesBySeason(SQLInteraction interactionHelper, int season)
    {
        string query = $"SELECT * FROM matches WHERE Season = {season}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.Match> matches = ReadMatches(interactionHelper, reader);
        
        return matches;
    }
    
    public static DataTypes.Match GetMatchById(SQLInteraction interactionHelper, ulong MID)
    {
        string query = $"SELECT * FROM matches WHERE MatchID = {MID} LIMIT 1";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        DataTypes.Match match = ReadSingleMatch(interactionHelper, reader);
        
        return match;
    }
}