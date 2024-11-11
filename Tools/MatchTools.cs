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
    
    public static void CreateMatch(SQLInteraction interactionHelper, DataTypes.Match match)
    {
        string query = $"INSERT INTO matches (Player1ID, Player2ID, Player1Score, Player2Score, MatchDate, Complete, WinnerID, Season, MapPoolID) VALUES ({match.Player1ID}, {match.Player2ID}, {match.Player1Score}, {match.Player2Score}, '{match.MatchDate.ToString("yyyy-MM-dd HH:mm:ss")}', {match.Complete}, {match.WinnerID}, {match.Season}, {match.MapPoolID})";
        interactionHelper.SendCommand(query);
    }
    
    public static void UpdateMatch(SQLInteraction interactionHelper, DataTypes.Match match)
    {
        string query = $"UPDATE matches SET Player1ID = {match.Player1ID}, Player2ID = {match.Player2ID}, Player1Score = {match.Player1Score}, Player2Score = {match.Player2Score}, MatchDate = '{match.MatchDate.ToString("yyyy-MM-dd HH:mm:ss")}', Complete = {match.Complete}, WinnerID = {match.WinnerID}, Season = {match.Season}, MapPoolID = {match.MapPoolID} WHERE MatchID = {match.MID}";
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteMatch(SQLInteraction interactionHelper, ulong MID)
    {
        string query = $"DELETE FROM matches WHERE MatchID = {MID}";
        interactionHelper.SendCommand(query);
    }
}