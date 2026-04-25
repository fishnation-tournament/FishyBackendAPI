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

    private static QualifierScore ReadSingleQualifierScore(MySqlConnection conn, MySqlDataReader reader)
    {
        QualifierScore score = new QualifierScore();
        if (reader.Read())
        {
            score = new QualifierScore(reader);
        }
        reader.Close();

        if (score.ScoresaberID != 0)
        {
            score.Player = PlayerTools.GetPlayerByScoresaberID(conn, score.ScoresaberID);
        }

        return score;
    }

    private static List<QualifierScore> ReadQualifierScores(MySqlConnection conn, MySqlDataReader reader)
    {
        List<QualifierScore> scores = new List<QualifierScore>();
        while (reader.Read())
        {
            scores.Add(new QualifierScore(reader));
        }
        reader.Close();

        for (int i = 0; i < scores.Count; i++)
        {
            var temp = scores[i];
            temp.Player = PlayerTools.GetPlayerByScoresaberID(conn, scores[i].ScoresaberID);
            Console.WriteLine($"Read qualifier score: ScoreID={temp.ScoreID}, ScoresaberID={temp.ScoresaberID}, PlayerID={temp.Player?.PlayerID}");
            Console.WriteLine($"Player scoresaberID: {scores[i].ScoresaberID}");
            scores[i] = temp;
        }

        return scores;
    }

    public static List<MatchScore> GetMatchScores(MySqlConnection dbConn, ulong MatchID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM player_scores WHERE MatchID = @MatchID", dbConn);
        cmd.Parameters.AddWithValue("@MatchID", MatchID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MatchScore> scores = ReadMatchScores(reader);

        reader.Close();
        return scores;
    }

    public static List<QualifierScore> GetQualifierScores(MySqlConnection dbConn, ulong QualifierID)
    {
        MySqlCommand cmd = new MySqlCommand("""
            SELECT qs.*
            FROM qualifier_scores qs
            INNER JOIN (
                SELECT ScoresaberID, MapID, MAX(Score) AS TopScore
                FROM qualifier_scores
                WHERE QualifierID = @QualifierID
                GROUP BY ScoresaberID, MapID
            ) best ON qs.ScoresaberID = best.ScoresaberID
                  AND qs.MapID = best.MapID
                  AND qs.Score = best.TopScore
            WHERE qs.QualifierID = @QualifierID
            """, dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadQualifierScores(dbConn, reader);
    }

    public static List<QualifierScore> GetQualifierScoreByMapID(MySqlConnection dbConn, ulong QualifierID, ulong MapID)
    {
        MySqlCommand cmd = new MySqlCommand("""
            SELECT qs.* FROM qualifier_scores qs
            INNER JOIN (
                SELECT ScoresaberID, MAX(Score) AS TopScore
                FROM qualifier_scores
                WHERE QualifierID = @QualifierID AND MapID = @MapID
                GROUP BY ScoresaberID
            ) best ON qs.ScoresaberID = best.ScoresaberID AND qs.Score = best.TopScore
            WHERE qs.QualifierID = @QualifierID AND qs.MapID = @MapID
            """, dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        cmd.Parameters.AddWithValue("@MapID", MapID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadQualifierScores(dbConn, reader);
    }

    public static List<QualifierScore> GetQualifierScoresByScoresaberID(MySqlConnection dbConn, ulong QualifierID, ulong ScoresaberID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM qualifier_scores WHERE ScoresaberID = @ScoresaberID AND QualifierID = @QualifierID",
            dbConn);
        cmd.Parameters.AddWithValue("@ScoresaberID", ScoresaberID);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadQualifierScores(dbConn, reader);
    }

    public static QualifierScore GetQualifierScoreByQualifierID(MySqlConnection dbConn, ulong QualifierID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM qualifier_scores WHERE QualifierID = @QualifierID", dbConn);
        cmd.Parameters.AddWithValue("@QualifierID", QualifierID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadSingleQualifierScore(dbConn, reader);
    }

    public static QualifierScore GetQualifierScoreByScoresaberIDAndMapID(MySqlConnection dbConn, ulong ScoresaberID, ulong MapID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM qualifier_scores WHERE ScoresaberID = @ScoresaberID AND MapID = @MapID LIMIT 1",
            dbConn);
        cmd.Parameters.AddWithValue("@ScoresaberID", ScoresaberID);
        cmd.Parameters.AddWithValue("@MapID", MapID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadSingleQualifierScore(dbConn, reader);
    }

    public static MatchScore? CreateMatchScore(MySqlConnection dbConn, FrontFacingMatchScore score)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO player_scores
            (UID, MapID, MatchID, Score, MaxScore, Misses, IsReplayAttempt)
        VALUES
            (@UID, @MapID, @MatchID, @Score, @MaxScore, @Misses, @IsReplayAttempt)
        """, dbConn);
        cmd.Parameters.AddWithValue("@UID", score.UID);
        cmd.Parameters.AddWithValue("@MapID", score.MapID);
        cmd.Parameters.AddWithValue("@MatchID", score.MatchID);
        cmd.Parameters.AddWithValue("@Score", score.Score);
        cmd.Parameters.AddWithValue("@MaxScore", score.MaxScore);
        cmd.Parameters.AddWithValue("@Misses", score.Misses);
        cmd.Parameters.AddWithValue("@IsReplayAttempt", score.IsReplayAttempt ? 1 : 0);
        cmd.ExecuteNonQuery();

        return GetMatchScoreById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static MatchScore? GetMatchScoreById(MySqlConnection dbConn, ulong scoreId)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM player_scores WHERE ScoreID = @ScoreID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@ScoreID", scoreId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new MatchScore(reader);
    }

    public static MatchScore? UpdateMatchScore(MySqlConnection dbConn, ulong scoreId, UpdateMatchScoreRequest request)
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE player_scores SET Score = @Score, MaxScore = @MaxScore, Misses = @Misses
        WHERE ScoreID = @ScoreID
        """, dbConn);
        cmd.Parameters.AddWithValue("@Score", request.Score);
        cmd.Parameters.AddWithValue("@MaxScore", request.MaxScore);
        cmd.Parameters.AddWithValue("@Misses", request.Misses);
        cmd.Parameters.AddWithValue("@ScoreID", scoreId);
        if (cmd.ExecuteNonQuery() == 0) return null;
        return GetMatchScoreById(dbConn, scoreId);
    }

    /// <summary>
    /// Marks all scores for the most recently played map in the given match with ReplayCalledAfter = 1.
    /// The scores remain valid reference points — the replaying player must beat them.
    /// Returns the flagged score entries, or an empty list if there are no scores.
    /// </summary>
    public static List<MatchScore> FlagPreReplayScores(MySqlConnection dbConn, ulong matchId)
    {
        MySqlCommand findCmd = new MySqlCommand(
                "SELECT MapID FROM player_scores WHERE MatchID = @MatchID AND ReplayCalledAfter = 0 ORDER BY ScoreID DESC LIMIT 1",
            dbConn);
        findCmd.Parameters.AddWithValue("@MatchID", matchId);
        object? result = findCmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return new List<MatchScore>();
        ulong mapId = Convert.ToUInt64(result);

        MySqlCommand flagCmd = new MySqlCommand(
            "UPDATE player_scores SET ReplayCalledAfter = 1 WHERE MatchID = @MatchID AND MapID = @MapID",
            dbConn);
        flagCmd.Parameters.AddWithValue("@MatchID", matchId);
        flagCmd.Parameters.AddWithValue("@MapID", mapId);
        flagCmd.ExecuteNonQuery();

        MySqlCommand getCmd = new MySqlCommand(
            "SELECT * FROM player_scores WHERE MatchID = @MatchID AND MapID = @MapID",
            dbConn);
        getCmd.Parameters.AddWithValue("@MatchID", matchId);
        getCmd.Parameters.AddWithValue("@MapID", mapId);
        using MySqlDataReader reader = getCmd.ExecuteReader();
        return ReadMatchScores(reader);
    }

    public static bool DeleteMatchScore(MySqlConnection dbConn, ulong scoreId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM player_scores WHERE ScoreID = @ScoreID", dbConn);
        cmd.Parameters.AddWithValue("@ScoreID", scoreId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public static bool DeleteQualifierScore(MySqlConnection dbConn, ulong scoreId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM qualifier_scores WHERE ScoreID = @ScoreID", dbConn);
        cmd.Parameters.AddWithValue("@ScoreID", scoreId);
        return cmd.ExecuteNonQuery() > 0;
    }

    public static void CreateQualifierScore(MySqlConnection dbConn, FrontFacingQualifierScore score)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO qualifier_scores
            (ScoresaberID, MapID, Score, MaxScore, Misses, QualifierID, SubmittedAt)
        VALUES
            (@ScoresaberID, @MapID, @Score, @MaxScore, @Misses, @QualifierID, UTC_TIMESTAMP())
        """, dbConn);
        cmd.Parameters.AddWithValue("@ScoresaberID", score.ScoresaberID);
        cmd.Parameters.AddWithValue("@MapID", score.MapID);
        cmd.Parameters.AddWithValue("@QualifierID", score.QualifierID);
        cmd.Parameters.AddWithValue("@Score", score.Score);
        cmd.Parameters.AddWithValue("@MaxScore", score.MaxScore);
        cmd.Parameters.AddWithValue("@Misses", score.Misses);
        cmd.ExecuteNonQuery();
    }
}
