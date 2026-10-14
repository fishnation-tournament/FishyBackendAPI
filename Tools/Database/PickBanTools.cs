using System.Text.Json;
using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public class PickBanTools
{
    private const string PickBanSelect =
        """
        SELECT pb.pick_ban_id AS PickBanID,
               pb.match_id    AS MatchID,
               pb.pick        AS Pick,
               pb.player_id   AS PlayerID,
               pb.team_id     AS TeamID,
               pb.map_id      AS MapRefID,
               m.MapID, m.BSMapID, m.MapName, m.MapImageLink, m.MPID, m.Season, m.Difficulty, m.Tags,
               f.flair_id AS FlairID, f.color AS FlairColor, f.name AS FlairName
        FROM pick_ban pb
        LEFT JOIN maps m ON m.MapID = pb.map_id
        LEFT JOIN flairs f ON m.flair = f.flair_id
        """;

    public static List<PickBan> GetPickBansByMatch(MySqlConnection dbConn, ulong matchId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"{PickBanSelect} WHERE pb.match_id = @MatchID ORDER BY pb.pick_ban_id",
            dbConn);
        cmd.Parameters.AddWithValue("@MatchID", matchId);
        using MySqlDataReader reader = cmd.ExecuteReader();

        List<PickBan> entries = new List<PickBan>();
        while (reader.Read())
            entries.Add(new PickBan(reader));
        return entries;
    }

    public static PickBan? GetPickBanById(MySqlConnection dbConn, int pickBanId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"{PickBanSelect} WHERE pb.pick_ban_id = @ID LIMIT 1",
            dbConn);
        cmd.Parameters.AddWithValue("@ID", pickBanId);
        using MySqlDataReader reader = cmd.ExecuteReader();

        if (!reader.Read()) return null;
        return new PickBan(reader);
    }

    public static PickBan CreatePickBan(MySqlConnection dbConn, CreatePickBanRequest request)
    {
        Console.WriteLine(JsonSerializer.Serialize(request));
        
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO pick_ban (match_id, pick, player_id, team_id, map_id) VALUES (@MatchID, @IsPick, @PlayerID, @TeamID, @MapID)",
            dbConn);
        cmd.Parameters.AddWithValue("@MatchID", request.MatchID);
        cmd.Parameters.AddWithValue("@IsPick", request.IsPick ? 1 : 0);
        cmd.Parameters.AddWithValue("@PlayerID", request.PlayerID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TeamID", request.TeamID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MapID", request.MapID);
        cmd.ExecuteNonQuery();

        int newId = (int)cmd.LastInsertedId;
        return GetPickBanById(dbConn, newId)!.Value;
    }

    public static bool DeletePickBan(MySqlConnection dbConn, int pickBanId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM pick_ban WHERE pick_ban_id = @ID",
            dbConn);
        cmd.Parameters.AddWithValue("@ID", pickBanId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
