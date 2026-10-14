using System.Text.Json.Serialization;

namespace FishyAPI.Tools.DataTypes;

// Each enum mirrors a CHECK constraint in SQLSchema.sql. JSON uses the same strings as the database,
// so the API accepts and returns e.g. "per_match", and unknown values are rejected with a 400 at binding.

/// <summary>map_pools.PoolType</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PoolType>))]
public enum PoolType
{
    /// <summary>A shared, curated pool used across a season's matches.</summary>
    [JsonStringEnumMemberName("season")] Season,
    /// <summary>A pool generated for a single match (see season_settings.MapPoolMode = per_match).</summary>
    [JsonStringEnumMemberName("match")] Match
}

/// <summary>season_settings.ParticipantType</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParticipantType>))]
public enum ParticipantType
{
    [JsonStringEnumMemberName("solo")] Solo,
    [JsonStringEnumMemberName("team")] Team
}

/// <summary>season_settings.MapPoolMode</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MapPoolMode>))]
public enum MapPoolMode
{
    /// <summary>Matches use pre-built season pools.</summary>
    [JsonStringEnumMemberName("predefined")] Predefined,
    /// <summary>Each match gets its own generated pool.</summary>
    [JsonStringEnumMemberName("per_match")] PerMatch
}

/// <summary>Converts the enums above to and from their database column values.</summary>
public static class EnumDbValues
{
    public static string ToDb(this PoolType value) => value switch
    {
        PoolType.Season => "season",
        PoolType.Match => "match",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static PoolType ParsePoolType(string value) => value switch
    {
        "season" => PoolType.Season,
        "match" => PoolType.Match,
        _ => throw new ArgumentException($"Unknown PoolType '{value}'.", nameof(value))
    };

    public static string ToDb(this ParticipantType value) => value switch
    {
        ParticipantType.Solo => "solo",
        ParticipantType.Team => "team",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static ParticipantType ParseParticipantType(string value) => value switch
    {
        "solo" => ParticipantType.Solo,
        "team" => ParticipantType.Team,
        _ => throw new ArgumentException($"Unknown ParticipantType '{value}'.", nameof(value))
    };

    public static string ToDb(this MapPoolMode value) => value switch
    {
        MapPoolMode.Predefined => "predefined",
        MapPoolMode.PerMatch => "per_match",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    public static MapPoolMode ParseMapPoolMode(string value) => value switch
    {
        "predefined" => MapPoolMode.Predefined,
        "per_match" => MapPoolMode.PerMatch,
        _ => throw new ArgumentException($"Unknown MapPoolMode '{value}'.", nameof(value))
    };
}
