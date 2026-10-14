using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public static class MatchLineupTools
{
    private const string SelectColumns = "LineupID, MatchID, MapID, TeamID, UserID";

    private static List<MatchLineup> ReadLineups(MySqlDataReader reader)
    {
        List<MatchLineup> lineups = new();
        while (reader.Read())
            lineups.Add(new MatchLineup(reader));
        return lineups;
    }

    public static List<MatchLineup> GetLineupsByMatch(MySqlConnection conn, ulong matchId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM match_lineups WHERE MatchID = @MatchID ORDER BY MapID, TeamID, LineupID",
            conn);
        cmd.Parameters.AddWithValue("@MatchID", matchId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadLineups(reader);
    }

    public static MatchLineup? GetLineupById(MySqlConnection conn, ulong lineupId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM match_lineups WHERE LineupID = @LineupID LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@LineupID", lineupId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new MatchLineup(reader);
    }

    public static int CountLineupEntries(MySqlConnection conn, ulong matchId, ulong mapId, int teamId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM match_lineups WHERE MatchID = @MatchID AND MapID = @MapID AND TeamID = @TeamID",
            conn);
        cmd.Parameters.AddWithValue("@MatchID", matchId);
        cmd.Parameters.AddWithValue("@MapID", mapId);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static MatchLineup CreateLineup(MySqlConnection conn, CreateMatchLineupRequest request)
    {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO match_lineups (MatchID, MapID, TeamID, UserID) VALUES (@MatchID, @MapID, @TeamID, @UserID)",
            conn);
        cmd.Parameters.AddWithValue("@MatchID", request.MatchID);
        cmd.Parameters.AddWithValue("@MapID", request.MapID);
        cmd.Parameters.AddWithValue("@TeamID", request.TeamID);
        cmd.Parameters.AddWithValue("@UserID", request.UserID);
        cmd.ExecuteNonQuery();
        return GetLineupById(conn, (ulong)cmd.LastInsertedId)!.Value;
    }

    public static bool DeleteLineup(MySqlConnection conn, ulong lineupId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM match_lineups WHERE LineupID = @LineupID", conn);
        cmd.Parameters.AddWithValue("@LineupID", lineupId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
