using MySql.Data.MySqlClient;
namespace FishyAPI.Tools;

public class ScoreTools
{
    public static List<DataTypes.MatchScore> ReadMatchScores(MySqlDataReader reader)
    {
        if(!reader.Read())
        {
            reader.Close();
            return new List<DataTypes.MatchScore>();
        }
        
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
        if(!reader.Read())
        {
            reader.Close();
            return new DataTypes.QualifierScore();
        }
        
        DataTypes.QualifierScore score = new DataTypes.QualifierScore();
        score.ScoreID = reader.GetUInt64(0);
        score.UID = reader.GetUInt64(1);
        score.MapID = reader.GetUInt64(2);
        score.Score = reader.GetInt32(3);
        score.MaxScore = reader.GetInt32(4);
        score.Misses = reader.GetInt32(5);
        score.QualifierID = reader.GetUInt64(6);
        reader.Close();
        
        return score;
    }
    
    public static List<DataTypes.QualifierScore> ReadQualifierScores(MySqlDataReader reader)
    {
        if(!reader.Read())
        {
            reader.Close();
            return new List<DataTypes.QualifierScore>();
        }
        
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
    public static List<DataTypes.MatchScore> GetMatchScores(SQLInteraction interactionHelper, ulong MID)
    {
        string query = $"SELECT * FROM player_scores WHERE MatchID = {MID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.MatchScore> scores = ReadMatchScores(reader);
        reader.Close();

        return scores;
    }
    
    public static List<DataTypes.QualifierScore> GetQualifierScores(SQLInteraction interactionHelper, ulong QID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE QualifierID = {QID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);
        reader.Close();
        
        return scores;
    }
    
    public static List<DataTypes.QualifierScore> GetQualifierScoreByMapID(SQLInteraction interactionHelper, ulong MapID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE MapID = {MapID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);
        reader.Close();
        
        return scores;
    }
    
    public static List<DataTypes.QualifierScore> GetQualifierScoresByUID(SQLInteraction interactionHelper, ulong UID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE UID = {UID}";
        MySqlDataReader reader = interactionHelper.GetReader(query);
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);
        reader.Close();
        
        return scores;
    }
    
    public static DataTypes.QualifierScore GetQualifierScoreByQualifierID(SQLInteraction interactionHelper, ulong QID)
    {
        string query = $"SELECT * FROM qualifier_scores WHERE QualifierID = {QID} LIMIT 1";
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
}