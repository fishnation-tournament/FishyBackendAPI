using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
namespace FishyAPI.Tools;

public class MatchTools
{
    public static List<Match> ReadMatches(MySqlConnection dbConn, MySqlDataReader reader)
    {
        List<Match> matches = new List<Match>();
        while (reader.Read())
        {
            matches.Add(new Match(reader));
        }
        reader.Close();

        for (int i = 0; i < matches.Count; i++)
        {
            var tempMatch = matches[i];
            tempMatch.Scores = ScoreTools.GetMatchScores(dbConn, tempMatch.MatchID);
            matches[i] = tempMatch;
        }
        return matches;
    }

    public static Match ReadSingleMatch(MySqlConnection dbConn, MySqlDataReader reader)
    {
        Match match = new Match();

        if (reader.Read())
        {
            match = new Match(reader);
        }
        reader.Close();

        match.Scores = ScoreTools.GetMatchScores(dbConn, match.MatchID);
        return match;
    }

    public static List<Match> GetMatches(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM matches", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Match> matches = ReadMatches(dbConn, reader);

        return matches;
    }

    public static List<Match> GetMatchesBySeason(MySqlConnection dbConn, int season)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM matches WHERE Season = @season", dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Match> matches = ReadMatches(dbConn, reader);

        return matches;
    }

    public static Match GetMatchById(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM matches WHERE MatchID = @MID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        Match match = ReadSingleMatch(dbConn, reader);

        return match;
    }

    public static Match CreateMatch(MySqlConnection dbConn, FrontFacingMatch frontFacingMatch)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO matches
            (Player1UID, Player2UID, Player1Points, Player2Points, MatchDateTime, MatchComplete, WinnerUID, Season, MapPoolID)
        VALUES
            (@Player1UID, @Player2UID, @Player1Points, @Player2Points, @MatchDateTime, @MatchComplete, @WinnerUID, @Season, @MapPoolID)
        """, dbConn);
        cmd.Parameters.AddWithValue("@Player1UID", frontFacingMatch.Player1ID);
        cmd.Parameters.AddWithValue("@Player2UID", frontFacingMatch.Player2ID);
        cmd.Parameters.AddWithValue("@Player1Points", frontFacingMatch.Player1Score);
        cmd.Parameters.AddWithValue("@Player2Points", frontFacingMatch.Player2Score);
        cmd.Parameters.AddWithValue("@MatchDateTime", frontFacingMatch.MatchDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@MatchComplete", frontFacingMatch.Complete);
        cmd.Parameters.AddWithValue("@WinnerUID", frontFacingMatch.WinnerID == 0 ? (object)DBNull.Value : frontFacingMatch.WinnerID);
        cmd.Parameters.AddWithValue("@Season", frontFacingMatch.Season);
        cmd.Parameters.AddWithValue("@MapPoolID", frontFacingMatch.MapPoolID);
        cmd.ExecuteNonQuery();
        return GetMatchById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static void UpdateMatch(MySqlConnection dbConn, Match match)
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE matches SET
            Player1UID = @Player1UID,
            Player2UID = @Player2UID,
            Player1Points = @Player1Points,
            Player2Points = @Player2Points,
            MatchDateTime = @MatchDateTime,
            MatchComplete = @MatchComplete,
            WinnerUID = @WinnerUID,
            Season = @Season,
            MapPoolID = @MapPoolID
        WHERE MatchID = @MatchID
        """, dbConn);
        cmd.Parameters.AddWithValue("@Player1UID", match.Player1ID);
        cmd.Parameters.AddWithValue("@Player2UID", match.Player2ID);
        cmd.Parameters.AddWithValue("@Player1Points", match.Player1Score ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player2Points", match.Player2Score ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchDateTime", match.MatchDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchComplete", match.Complete);
        cmd.Parameters.AddWithValue("@WinnerUID", match.WinnerID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Season", match.Season);
        cmd.Parameters.AddWithValue("@MapPoolID", match.MapPoolID);
        cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteMatch(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM matches WHERE MatchID = @MID", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        cmd.ExecuteNonQuery();
    }
}
