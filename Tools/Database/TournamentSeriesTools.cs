using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public static class TournamentSeriesTools
{
    private const string SelectColumns = "SeriesID, SeriesName, Slug, Description";

    private static List<TournamentSeries> ReadSeries(MySqlDataReader reader)
    {
        List<TournamentSeries> series = new();
        while (reader.Read())
            series.Add(new TournamentSeries(reader));
        return series;
    }

    public static List<TournamentSeries> GetAllSeries(MySqlConnection conn)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM tournament_series ORDER BY SeriesID",
            conn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadSeries(reader);
    }

    public static TournamentSeries? GetSeriesById(MySqlConnection conn, int seriesId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM tournament_series WHERE SeriesID = @SeriesID LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@SeriesID", seriesId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new TournamentSeries(reader);
    }

    public static TournamentSeries? GetSeriesBySlug(MySqlConnection conn, string slug)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM tournament_series WHERE Slug = @Slug LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new TournamentSeries(reader);
    }

    public static TournamentSeries CreateSeries(MySqlConnection conn, CreateTournamentSeriesRequest request)
    {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO tournament_series (SeriesName, Slug, Description) VALUES (@SeriesName, @Slug, @Description)",
            conn);
        cmd.Parameters.AddWithValue("@SeriesName", request.SeriesName);
        cmd.Parameters.AddWithValue("@Slug", request.Slug);
        cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
        return GetSeriesById(conn, (int)cmd.LastInsertedId)!.Value;
    }

    public static TournamentSeries? UpdateSeries(MySqlConnection conn, int seriesId, UpdateTournamentSeriesRequest request)
    {
        TournamentSeries? existing = GetSeriesById(conn, seriesId);
        if (!existing.HasValue) return null;

        MySqlCommand cmd = new MySqlCommand(
            "UPDATE tournament_series SET SeriesName = @SeriesName, Slug = @Slug, Description = @Description WHERE SeriesID = @SeriesID",
            conn);
        cmd.Parameters.AddWithValue("@SeriesName", request.SeriesName ?? existing.Value.SeriesName);
        cmd.Parameters.AddWithValue("@Slug", request.Slug ?? existing.Value.Slug);
        cmd.Parameters.AddWithValue("@Description", (object?)(request.Description ?? existing.Value.Description) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SeriesID", seriesId);
        cmd.ExecuteNonQuery();
        return GetSeriesById(conn, seriesId);
    }

    public static bool DeleteSeries(MySqlConnection conn, int seriesId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM tournament_series WHERE SeriesID = @SeriesID", conn);
        cmd.Parameters.AddWithValue("@SeriesID", seriesId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
