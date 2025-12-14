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

    public static List<DataTypes.MatchScore> GetMatchScores(MySqlConnection dbConn, ulong MatchID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM player_scores WHERE MatchID = @MatchID", dbConn);
        cmd.Parameters.AddWithValue("@MatchID", MatchID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.MatchScore> scores = ReadMatchScores(reader);

        return scores;
    }

    public static List<DataTypes.QualifierScore> GetQualifierScores(MySqlConnection dbConn, ulong QualifierID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);

        return scores;
    }

    public static List<DataTypes.QualifierScore> GetQualifierScoreByMapID(MySqlConnection dbConn, ulong QualifierID, ulong MapID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE MapID = @MapID AND QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@MapID", MapID);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);

        return scores;
    }

    public static List<DataTypes.QualifierScore> GetQualifierScoresByUID(MySqlConnection dbConn, ulong QualifierID, ulong UID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE UID = @UID AND QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.QualifierScore> scores = ReadQualifierScores(reader);

        return scores;
    }

    public static DataTypes.QualifierScore GetQualifierScoreByQualifierID(MySqlConnection dbConn, ulong QualifierID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.QualifierScore score = ReadSingleQualifierScore(reader);

        return score;
    }

    public static DataTypes.QualifierScore GetQualifierScoreByUIDAndMapID(MySqlConnection dbConn, ulong UID, ulong MapID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE UID = @UID AND MapID = @MapID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        cmd.Parameters.AddWithValue("@MapID", MapID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.QualifierScore score = ReadSingleQualifierScore(reader);

        return score;
    }

    public static void CreateMatchScore(MySqlConnection dbConn, FrontFacingDataTypes.FrontFacingMatchScore score)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO player_scores
            (UID, MapID, MatchID, Score, MaxScore, Misses)
        VALUES
            (@UID, @MapID, @MatchID, @Score, @MaxScore, @Misses)
        """, dbConn);
        cmd.Parameters.AddWithValue("@UID", score.UID);
        cmd.Parameters.AddWithValue("@MapID", score.MapID);
        cmd.Parameters.AddWithValue("@MatchID", score.MatchID);
        cmd.Parameters.AddWithValue("@Score", score.Score);
        cmd.Parameters.AddWithValue("@MaxScore", score.MaxScore);
        cmd.Parameters.AddWithValue("@Misses", score.Misses);
        cmd.ExecuteNonQuery();
    }

    public static void CreateQualifierScore(MySqlConnection dbConn, FrontFacingDataTypes.FrontFacingQualifierScore score)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO qualifier_scores
            (UID, MapID, QualifierID, Score, MaxScore, Misses)
        VALUES
            (@UID, @MapID, @QualifierID, @Score, @MaxScore, @Misses)
        """, dbConn);
        cmd.Parameters.AddWithValue("@UID", score.UID);
        cmd.Parameters.AddWithValue("@MapID", score.MapID);
        cmd.Parameters.AddWithValue("@QualifierID", score.QualifierID);
        cmd.Parameters.AddWithValue("@Score", score.Score);
        cmd.Parameters.AddWithValue("@MaxScore", score.MaxScore);
        cmd.Parameters.AddWithValue("@Misses", score.Misses);
        cmd.ExecuteNonQuery();
    }
}