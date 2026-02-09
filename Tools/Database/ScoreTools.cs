using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
namespace FishyAPI.Tools;

public class ScoreTools
{
    public static List<MatchScore> ReadMatchScores(MySqlDataReader reader)
    {
        List<MatchScore> scores = new List<MatchScore>();
        while (reader.Read())
        {
            scores.Add(new MatchScore(reader));
        }

        return scores;
    }

    public static QualifierScore ReadSingleQualifierScore(MySqlDataReader reader)
    {
        QualifierScore score = new QualifierScore();
        if (reader.Read())
        {
            score = new QualifierScore(reader);
        }

        return score;
    }

    public static List<QualifierScore> ReadQualifierScores(MySqlDataReader reader)
    {
        List<QualifierScore> scores = new List<QualifierScore>();
        while (reader.Read())
        {
            scores.Add(new QualifierScore(reader));
        }

        return scores;
    }

    public static List<MatchScore> GetMatchScores(MySqlConnection dbConn, ulong MatchID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM player_scores WHERE MatchID = @MatchID", dbConn);
        cmd.Parameters.AddWithValue("@MatchID", MatchID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MatchScore> scores = ReadMatchScores(reader);

        return scores;
    }

    public static List<QualifierScore> GetQualifierScores(MySqlConnection dbConn, ulong QualifierID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<QualifierScore> scores = ReadQualifierScores(reader);

        return scores;
    }

    public static List<QualifierScore> GetQualifierScoreByMapID(MySqlConnection dbConn, ulong QualifierID, ulong MapID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE MapID = @MapID AND QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@MapID", MapID);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<QualifierScore> scores = ReadQualifierScores(reader);

        return scores;
    }

    public static List<QualifierScore> GetQualifierScoresByUID(MySqlConnection dbConn, ulong QualifierID, ulong UID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE UID = @UID AND QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<QualifierScore> scores = ReadQualifierScores(reader);

        return scores;
    }

    public static QualifierScore GetQualifierScoreByQualifierID(MySqlConnection dbConn, ulong QualifierID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        QualifierScore score = ReadSingleQualifierScore(reader);

        return score;
    }

    public static QualifierScore GetQualifierScoreByUIDAndMapID(MySqlConnection dbConn, ulong UID, ulong MapID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM qualifier_scores WHERE UID = @UID AND MapID = @MapID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@UID", UID);
        cmd.Parameters.AddWithValue("@MapID", MapID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        QualifierScore score = ReadSingleQualifierScore(reader);

        return score;
    }

    public static void CreateMatchScore(MySqlConnection dbConn, FrontFacingMatchScore score)
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

    public static void CreateQualifierScore(MySqlConnection dbConn, FrontFacingQualifierScore score)
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
