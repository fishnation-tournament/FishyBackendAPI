using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;

namespace FishyAPI.Tools;

public static class FlairTools
{
    public static List<Flair> GetAllFlairs(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT flair_id, color, name FROM flairs ORDER BY flair_id",
            dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Flair> flairs = new List<Flair>();
        while (reader.Read())
            flairs.Add(new Flair(reader));
        return flairs;
    }

    public static Flair? GetFlairById(MySqlConnection dbConn, int flairId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT flair_id, color, name FROM flairs WHERE flair_id = @ID LIMIT 1",
            dbConn);
        cmd.Parameters.AddWithValue("@ID", flairId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Flair(reader);
    }

    public static Flair CreateFlair(MySqlConnection dbConn, string color, string name)
    {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO flairs (color, name) VALUES (@Color, @Name)",
            dbConn);
        cmd.Parameters.AddWithValue("@Color", color);
        cmd.Parameters.AddWithValue("@Name", name);
        cmd.ExecuteNonQuery();
        return GetFlairById(dbConn, (int)cmd.LastInsertedId)!.Value;
    }

    public static Flair? UpdateFlair(MySqlConnection dbConn, int flairId, string? color, string? name)
    {
        Flair? existing = GetFlairById(dbConn, flairId);
        if (!existing.HasValue) return null;

        MySqlCommand cmd = new MySqlCommand(
            "UPDATE flairs SET color = @Color, name = @Name WHERE flair_id = @ID",
            dbConn);
        cmd.Parameters.AddWithValue("@Color", color ?? existing.Value.Color);
        cmd.Parameters.AddWithValue("@Name", name ?? existing.Value.Name);
        cmd.Parameters.AddWithValue("@ID", flairId);
        cmd.ExecuteNonQuery();
        return GetFlairById(dbConn, flairId);
    }

    public static bool DeleteFlair(MySqlConnection dbConn, int flairId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM flairs WHERE flair_id = @ID",
            dbConn);
        cmd.Parameters.AddWithValue("@ID", flairId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
