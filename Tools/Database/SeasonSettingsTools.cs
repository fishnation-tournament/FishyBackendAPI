using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public static class SeasonSettingsTools
{
    private const string SelectColumns =
        "SeasonID, BracketFormat, ParticipantType, TeamMaxSize, TeamActivePlayers, MapPoolMode";

    public static SeasonSettings? GetSettings(MySqlConnection conn, int seasonId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {SelectColumns} FROM season_settings WHERE SeasonID = @SeasonID LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@SeasonID", seasonId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new SeasonSettings(reader);
    }

    public static bool IsTeamSeason(MySqlConnection conn, int seasonId)
    {
        SeasonSettings? settings = GetSettings(conn, seasonId);
        return settings.HasValue && settings.Value.ParticipantType == ParticipantType.Team;
    }

    /// <summary>
    /// Merges the request over the existing settings (or the schema defaults when none exist yet).
    /// Mirrors the table's CHECK constraints so callers get a readable error instead of a SQL failure.
    /// Switching a season to 'solo' clears the team sizes, since the schema requires them to be NULL.
    /// </summary>
    public static (SeasonSettings? settings, string? error) MergeAndValidate(SeasonSettings? existing, int seasonId, UpsertSeasonSettingsRequest request)
    {
        SeasonSettings merged = existing ?? new SeasonSettings
        {
            SeasonID = seasonId,
            BracketFormat = "double_elim",
            ParticipantType = ParticipantType.Solo,
            TeamMaxSize = null,
            TeamActivePlayers = null,
            MapPoolMode = MapPoolMode.Predefined
        };

        if (request.BracketFormat != null) merged.BracketFormat = request.BracketFormat;
        if (request.ParticipantType.HasValue) merged.ParticipantType = request.ParticipantType.Value;
        if (request.TeamMaxSize.HasValue) merged.TeamMaxSize = request.TeamMaxSize;
        if (request.TeamActivePlayers.HasValue) merged.TeamActivePlayers = request.TeamActivePlayers;
        if (request.MapPoolMode.HasValue) merged.MapPoolMode = request.MapPoolMode.Value;

        if (string.IsNullOrWhiteSpace(merged.BracketFormat) || merged.BracketFormat.Length > 50)
            return (null, "BracketFormat must be 1-50 characters.");

        if (merged.ParticipantType == ParticipantType.Solo)
        {
            merged.TeamMaxSize = null;
            merged.TeamActivePlayers = null;
        }
        else
        {
            if (!merged.TeamMaxSize.HasValue || merged.TeamMaxSize.Value < 1)
                return (null, "Team seasons require TeamMaxSize >= 1.");
            if (!merged.TeamActivePlayers.HasValue || merged.TeamActivePlayers.Value < 1 || merged.TeamActivePlayers.Value > merged.TeamMaxSize.Value)
                return (null, "Team seasons require TeamActivePlayers between 1 and TeamMaxSize.");
        }

        return (merged, null);
    }

    public static SeasonSettings UpsertSettings(MySqlConnection conn, SeasonSettings settings)
    {
        MySqlCommand cmd = new MySqlCommand("""
            INSERT INTO season_settings (SeasonID, BracketFormat, ParticipantType, TeamMaxSize, TeamActivePlayers, MapPoolMode)
            VALUES (@SeasonID, @BracketFormat, @ParticipantType, @TeamMaxSize, @TeamActivePlayers, @MapPoolMode)
            ON DUPLICATE KEY UPDATE
                BracketFormat = VALUES(BracketFormat),
                ParticipantType = VALUES(ParticipantType),
                TeamMaxSize = VALUES(TeamMaxSize),
                TeamActivePlayers = VALUES(TeamActivePlayers),
                MapPoolMode = VALUES(MapPoolMode)
            """, conn);
        cmd.Parameters.AddWithValue("@SeasonID", settings.SeasonID);
        cmd.Parameters.AddWithValue("@BracketFormat", settings.BracketFormat);
        cmd.Parameters.AddWithValue("@ParticipantType", settings.ParticipantType.ToDb());
        cmd.Parameters.AddWithValue("@TeamMaxSize", settings.TeamMaxSize ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@TeamActivePlayers", settings.TeamActivePlayers ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MapPoolMode", settings.MapPoolMode.ToDb());
        cmd.ExecuteNonQuery();
        return GetSettings(conn, settings.SeasonID)!.Value;
    }

    public static bool DeleteSettings(MySqlConnection conn, int seasonId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM season_settings WHERE SeasonID = @SeasonID", conn);
        cmd.Parameters.AddWithValue("@SeasonID", seasonId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
