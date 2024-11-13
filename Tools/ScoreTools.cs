using MySql.Data.MySqlClient;
namespace FishyAPI.Tools;

public class ScoreTools
{
    public static List<DataTypes.MatchScore> ReadMatchScores(MySqlDataReader reader)
    {
        List<DataTypes.MatchScore> scores = new List<DataTypes.MatchScore>();
        while (reader.Read())
        {
            DataTypes.MatchScore score = new DataTypes.MatchScore();
            score.ScoreID = reader.GetUInt64(0);
            score.UID = reader.GetUInt64(1);
            score.MapID = reader.GetUInt64(2);
            score.MatchID = reader.GetUInt64(3);
            score.Score = reader.GetInt32(4);
            score.MaxScore = reader.GetInt32(5);
            score.Misses = reader.GetInt32(6);
            scores.Add(score);
        }
        reader.Close();
        
        return scores;
    }
    
    public static DataTypes.QualifierScore ReadSingleQualifierScore(MySqlDataReader reader)
    {
        DataTypes.QualifierScore score = new DataTypes.QualifierScore();
        while (reader.Read())
        {
            score.ScoreID = reader.GetUInt64(0);
            score.UID = reader.GetUInt64(1);
            score.MapID = reader.GetUInt64(2);
            score.Score = reader.GetInt32(3);
            score.MaxScore = reader.GetInt32(4);
            score.Misses = reader.GetInt32(5);
            score.QualifierID = reader.GetUInt64(6);
        }
        reader.Close();
        
        return score;
    }
    
    public static List<DataTypes.QualifierScore> ReadQualifierScores(MySqlDataReader reader)
    {
        List<DataTypes.QualifierScore> scores = new List<DataTypes.QualifierScore>();
        while (reader.Read())
        {
            DataTypes.QualifierScore score = new DataTypes.QualifierScore();
            score.ScoreID = reader.GetUInt64(0);
            score.UID = reader.GetUInt64(1);
            score.MapID = reader.GetUInt64(2);
            score.Score = reader.GetInt32(3);
            score.MaxScore = reader.GetInt32(4);
            score.Misses = reader.GetInt32(5);
            score.QualifierID = reader.GetUInt64(6);
            scores.Add(score);
        }
        reader.Close();
        
        return scores;
    }
    public static List<DataTypes.MatchScore> GetMatchScores(SQLInteraction interactionHelper, ulong MatchID)
    {
        string query = $"SELECT * FROM player_scores WHERE MatchID = {MatchID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.MatchScore> scores = ReadMatchScores(reader);
        reader.Close();

        return scores;
    }
    
    public static List<DataTypes.QualifierScore> GetQualifierScores(SQLInteraction interactionHelper, ulong QualifierID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE QualifierID = {QualifierID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);
        reader.Close();
        
        return scores;
    }
    
    public static List<DataTypes.QualifierScore> GetQualifierScoreByMapID(SQLInteraction interactionHelper, ulong QualifierID, ulong MapID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE MapID = {MapID} AND QualifierID = {QualifierID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);
        reader.Close();
        
        return scores;
    }
    
    public static List<DataTypes.QualifierScore> GetQualifierScoresByUID(SQLInteraction interactionHelper, ulong QualifierID, ulong UID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE UID = {UID} AND QualifierID = {QualifierID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);
        reader.Close();
        
        return scores;
    }
    
    public static DataTypes.QualifierScore GetQualifierScoreByQualifierID(SQLInteraction interactionHelper, ulong QualifierID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE QualifierID = {QualifierID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        DataTypes.QualifierScore score = ReadSingleQualifierScore(reader);
        reader.Close();
        
        return score;
    }

    public static DataTypes.QualifierScore GetQualifierScoreByUIDAndMapID(SQLInteraction interactionHelper, ulong UID, ulong MapID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE UID = {UID} AND MapID = {MapID} LIMIT 1";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        DataTypes.QualifierScore score = ReadSingleQualifierScore(reader);
        reader.Close();

        return score;
    }
    
    public static void CreateMatchScore(SQLInteraction interactionHelper, FrontFacingDataTypes.FfMatchScore score)
    {
        string query = $"INSERT INTO player_scores (UID, MapID, MatchID, Score, MaxScore, Misses) VALUES ({score.UID}, {score.MapID}, {score.MatchID}, {score.Score}, {score.MaxScore}, {score.Misses})";
        interactionHelper.SendCommand(query);
    }
    
    public static void CreateQualifierScore(SQLInteraction interactionHelper, FrontFacingDataTypes.FfQualifierScore score)
    {
        string query = $"INSERT INTO qualifier_scores (UID, MapID, QualifierID, Score, MaxScore, Misses) VALUES ({score.UID}, {score.MapID}, {score.QualifierID}, {score.Score}, {score.MaxScore}, {score.Misses})";
        interactionHelper.SendCommand(query);
    }
}