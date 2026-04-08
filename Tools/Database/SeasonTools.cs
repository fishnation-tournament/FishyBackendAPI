using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public static class SeasonTools
{
    private const string SelectColumns = "SeasonID, SeasonName, StartDate, EndDate, QualifierCutoff";

    private static List<Season> ReadSeasons(MySqlDataReader reader)
    {
        List<Season> seasons = new();
        while (reader.Read())
            seasons.Add(new Season(reader));
        return seasons;
    }

    public static List<Season> GetSeasons(MySqlConnection conn)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM seasons ORDER BY StartDate DESC",
            conn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadSeasons(reader);
    }

    public static Season? GetSeasonById(MySqlConnection conn, int seasonId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM seasons WHERE SeasonID = @SeasonID",
            conn);
        cmd.Parameters.AddWithValue("@SeasonID", seasonId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Season(reader);
    }

    public static Season? GetActiveSeason(MySqlConnection conn)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM seasons WHERE @Now BETWEEN StartDate AND EndDate LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Season(reader);
    }

    public static Season CreateSeason(MySqlConnection conn, CreateSeasonRequest request)
    {
        MySqlCommand cmd = new MySqlCommand("""
            INSERT INTO seasons (SeasonName, StartDate, EndDate, QualifierCutoff)
            VALUES (@SeasonName, @StartDate, @EndDate, @QualifierCutoff)
            """, conn);
        cmd.Parameters.AddWithValue("@SeasonName", request.SeasonName);
        cmd.Parameters.AddWithValue("@StartDate", request.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@EndDate", request.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@QualifierCutoff", request.QualifierCutoff.HasValue
            ? request.QualifierCutoff.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : (object)DBNull.Value);
        cmd.ExecuteNonQuery();
        return GetSeasonById(conn, (int)cmd.LastInsertedId)!.Value;
    }

    public static Season? UpdateSeason(MySqlConnection conn, int seasonId, UpdateSeasonRequest request)
    {
        var sets = new List<string>();
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = conn;

        if (request.SeasonName != null)
        {
            sets.Add("SeasonName = @SeasonName");
            cmd.Parameters.AddWithValue("@SeasonName", request.SeasonName);
        }
        if (request.StartDate.HasValue)
        {
            sets.Add("StartDate = @StartDate");
            cmd.Parameters.AddWithValue("@StartDate", request.StartDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        if (request.EndDate.HasValue)
        {
            sets.Add("EndDate = @EndDate");
            cmd.Parameters.AddWithValue("@EndDate", request.EndDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        if (request.ClearQualifierCutoff)
        {
            sets.Add("QualifierCutoff = NULL");
        }
        else if (request.QualifierCutoff.HasValue)
        {
            sets.Add("QualifierCutoff = @QualifierCutoff");
            cmd.Parameters.AddWithValue("@QualifierCutoff", request.QualifierCutoff.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        if (sets.Count > 0)
        {
            cmd.CommandText = $"UPDATE seasons SET {string.Join(", ", sets)} WHERE SeasonID = @SeasonID";
            cmd.Parameters.AddWithValue("@SeasonID", seasonId);
            cmd.ExecuteNonQuery();
        }

        return GetSeasonById(conn, seasonId);
    }

    public static bool DeleteSeason(MySqlConnection conn, int seasonId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM seasons WHERE SeasonID = @SeasonID", conn);
        cmd.Parameters.AddWithValue("@SeasonID", seasonId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
