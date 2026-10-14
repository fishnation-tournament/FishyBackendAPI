using System.Text.Json.Serialization;
using FishyAPI.Tools.DataTypes;

namespace FishyAPI.Tools.MatchFlow;

// The server-computed state of a match. Clients render this; they never derive results themselves.

[JsonConverter(typeof(JsonStringEnumConverter<MatchStatus>))]
public enum MatchStatus
{
    [JsonStringEnumMemberName("not_started")] NotStarted,
    [JsonStringEnumMemberName("in_progress")] InProgress,
    [JsonStringEnumMemberName("complete")] Complete,
    [JsonStringEnumMemberName("forfeit")] Forfeit,
}

[JsonConverter(typeof(JsonStringEnumConverter<StepAction>))]
public enum StepAction
{
    [JsonStringEnumMemberName("ban")] Ban,
    [JsonStringEnumMemberName("pick")] Pick,
}

[JsonConverter(typeof(JsonStringEnumConverter<NextActionType>))]
public enum NextActionType
{
    /// <summary>Nothing to do: complete, forfeited, or the match isn't configured (see Message).</summary>
    [JsonStringEnumMemberName("none")] None,
    [JsonStringEnumMemberName("ban")] Ban,
    [JsonStringEnumMemberName("pick")] Pick,
    /// <summary>Team match: the lineup for the current map must be set before scores.</summary>
    [JsonStringEnumMemberName("set_lineup")] SetLineup,
    [JsonStringEnumMemberName("submit_scores")] SubmitScores,
    /// <summary>Even pool: the coordinator must enter the secret tiebreaker map.</summary>
    [JsonStringEnumMemberName("set_tiebreaker")] SetTiebreaker,
    /// <summary>The deciding map was lost by a side that still has its replay: call it, or confirm the result.</summary>
    [JsonStringEnumMemberName("replay_or_confirm")] ReplayOrConfirm,
}

[JsonConverter(typeof(JsonStringEnumConverter<PoolMapStatus>))]
public enum PoolMapStatus
{
    [JsonStringEnumMemberName("available")] Available,
    [JsonStringEnumMemberName("banned")] Banned,
    [JsonStringEnumMemberName("picked")] Picked,
    [JsonStringEnumMemberName("tiebreaker")] Tiebreaker,
}

[JsonConverter(typeof(JsonStringEnumConverter<GameStatus>))]
public enum GameStatus
{
    [JsonStringEnumMemberName("awaiting_lineup")] AwaitingLineup,
    [JsonStringEnumMemberName("awaiting_scores")] AwaitingScores,
    [JsonStringEnumMemberName("replaying")] Replaying,
    [JsonStringEnumMemberName("settled")] Settled,
}

[JsonConverter(typeof(JsonStringEnumConverter<AttemptKind>))]
public enum AttemptKind
{
    /// <summary>The first play of the map.</summary>
    [JsonStringEnumMemberName("initial")] Initial,
    /// <summary>A replay called by the loser: each player's best of this and the previous attempt counts.</summary>
    [JsonStringEnumMemberName("replay")] Replay,
    /// <summary>The previous attempt was an exact tie: only this attempt counts and no replay is used.</summary>
    [JsonStringEnumMemberName("tie_replay")] TieReplay,
}

public sealed class MatchState
{
    public ulong MatchId { get; set; }
    /// <summary>Changes whenever anything in the match changes. Send it back as expectedVersion with every action.</summary>
    public string Version { get; set; } = "";
    public MatchStatus Status { get; set; }
    public int? BestOf { get; set; }
    public int? WinsNeeded { get; set; }
    public bool IsTeamMatch { get; set; }
    public FormatInfo Format { get; set; } = new();
    public List<SideState> Sides { get; set; } = new();
    public int? WinnerSide { get; set; }
    public NextAction Next { get; set; } = new();
    /// <summary>Set while the loser of the most recent map may still call their replay.</summary>
    public ReplayWindow? ReplayWindow { get; set; }
    public List<SequenceStepState> Sequence { get; set; } = new();
    public List<PoolMapState> Maps { get; set; } = new();
    /// <summary>Maps actually played, in play order.</summary>
    public List<GameState> Games { get; set; } = new();
    /// <summary>True for older matches whose stored points/winner don't match what their records add up to.</summary>
    public bool StoredResultDiffers { get; set; }
    /// <summary>Recorded data that doesn't follow the rules (only expected on matches played before the guarded flow).</summary>
    public List<string> Deviations { get; set; } = new();

    // ── Server-side only (persisted, not sent) ─────────────────────────────
    [JsonIgnore] public HashSet<ulong> ReplayAttemptScoreIds { get; } = new();
    [JsonIgnore] public HashSet<ulong> ReplayCalledAfterScoreIds { get; } = new();
    [JsonIgnore] public bool FinalizedInput { get; set; }
    [JsonIgnore] public bool LastGameIsLatestAction { get; set; }
}

public sealed class FormatInfo
{
    public int PoolSize { get; set; }
    public int? Bans { get; set; }
    public int? Picks { get; set; }
    /// <summary>"pool" (the leftover map) or "secret" (even pool: a map entered by the coordinator).</summary>
    public string? TiebreakerSource { get; set; }
    public bool Supported { get; set; }
    public string? Problem { get; set; }
}

public sealed class SideState
{
    public int Side { get; set; }
    public ulong? UserId { get; set; }
    public int? TeamId { get; set; }
    public string? Name { get; set; }
    public int Points { get; set; }
    public bool ReplayUsed { get; set; }
    public bool ReplayAvailable { get; set; }
    public bool IsWinner { get; set; }
}

public sealed class NextAction
{
    public NextActionType Action { get; set; }
    /// <summary>Which side acts (ban/pick/replay), or null when both are involved.</summary>
    public int? Side { get; set; }
    /// <summary>1-based position in the ban/pick sequence (ban/pick only).</summary>
    public int? Step { get; set; }
    public ulong? MapId { get; set; }
    public int? Attempt { get; set; }
    public AttemptKind? AttemptKind { get; set; }
    /// <summary>User IDs whose scores (or lineup sides) are still needed.</summary>
    public List<ulong> AwaitingScoresFrom { get; set; } = new();
    public List<int> AwaitingLineupFromSides { get; set; } = new();
    public string? Message { get; set; }
}

public sealed class ReplayWindow
{
    public int Side { get; set; }
    public ulong MapId { get; set; }
    /// <summary>True if this map decided the match: the match waits for a replay or confirmation.</summary>
    public bool IsDecidingMap { get; set; }
}

public sealed class SequenceStepState
{
    public int Step { get; set; }
    public int Cycle { get; set; }
    public StepAction Action { get; set; }
    public int Side { get; set; }
    public bool Done { get; set; }
    public ulong? MapId { get; set; }
    public int? PickBanId { get; set; }
}

public sealed class PoolMapState
{
    public ulong MapId { get; set; }
    public Map Map { get; set; }
    public PoolMapStatus Status { get; set; }
    /// <summary>Side that banned or picked it.</summary>
    public int? Side { get; set; }
    public int? PlayOrder { get; set; }
}

public sealed class GameState
{
    public int Order { get; set; }
    public ulong MapId { get; set; }
    public Map? Map { get; set; }
    public bool IsTiebreaker { get; set; }
    public int? PickedBySide { get; set; }
    public GameStatus Status { get; set; }
    public int? WinnerSide { get; set; }
    public List<AttemptState> Attempts { get; set; } = new();
    public List<ulong> AwaitingScoresFrom { get; set; } = new();
    public List<int> AwaitingLineupFromSides { get; set; } = new();

    [JsonIgnore] public int? PickBanId { get; set; }
    [JsonIgnore] public List<int> ReplayCallers { get; } = new();
}

public sealed class AttemptState
{
    public int Number { get; set; }
    public AttemptKind Kind { get; set; }
    public int? CalledBySide { get; set; }
    public bool Complete { get; set; }
    public long? Side1Total { get; set; }
    public long? Side2Total { get; set; }
    public List<ScoreState> Scores { get; set; } = new();
}

public sealed class ScoreState
{
    public ulong ScoreId { get; set; }
    public ulong? UserId { get; set; }
    [JsonConverter(typeof(UInt64ToStringConverter))]
    public ulong ScoresaberId { get; set; }
    public int Side { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public int Misses { get; set; }
    /// <summary>Whether this score is the one that counted for the attempt's totals.</summary>
    public bool Counted { get; set; }
}
