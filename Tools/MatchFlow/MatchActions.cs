using System.Collections.Concurrent;
using System.Text.Json;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Tools.FrontFacingDataTypes;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools.MatchFlow;

public sealed record ActionError(int Status, string Message);

public sealed record ActionOutcome(int Status, string? Error, MatchState? State);

/// <summary>
/// Runs every change to a match through one pipeline:
///   lock the match → check expectedVersion → apply the change in a transaction → recompute with MatchEngine
///   → keep the bracket consistent → save the derived fields → commit → broadcast the new state.
/// Points, winner, completion, replay flags and ban/pick counts are only ever written from the engine's result.
/// </summary>
public static class MatchActions
{
    private static readonly ConcurrentDictionary<ulong, SemaphoreSlim> Locks = new();
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    // ── Loading ──────────────────────────────────────────────────────────────

    public static MatchContext? Load(MySqlConnection conn, ulong matchId)
    {
        DataTypes.Match match = MatchTools.GetMatchById(conn, matchId);
        if (match.MatchID == 0) return null;
        return new MatchContext
        {
            Match = match,
            Pool = MapTools.GetMapsInMapPool(conn, match.MapPoolID),
            TeamActivePlayers = SeasonSettingsTools.GetSettings(conn, match.Season)?.TeamActivePlayers,
        };
    }

    public static MatchState? GetState(MySqlConnection conn, ulong matchId)
    {
        MatchContext? ctx = Load(conn, matchId);
        return ctx == null ? null : MatchEngine.Evaluate(ctx);
    }

    // ── Pipeline ─────────────────────────────────────────────────────────────

    /// <param name="requireVersion">Guarded actions must send the version they acted on; overrides may omit it.</param>
    public static async Task<ActionOutcome> Run(ConnectionManager connManager, ulong matchId, string? expectedVersion, bool requireVersion,
        string actionName, Func<MySqlConnection, MatchContext, MatchState, ActionError?> apply)
    {
        if (requireVersion && string.IsNullOrWhiteSpace(expectedVersion))
            return new ActionOutcome(400, "expectedVersion is required (use the version from GET /Matches/{id}/State).", null);

        SemaphoreSlim matchLock = Locks.GetOrAdd(matchId, _ => new SemaphoreSlim(1, 1));
        await matchLock.WaitAsync();
        MatchState finalState;
        try
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MatchContext? ctx = Load(conn, matchId);
            if (ctx == null) return new ActionOutcome(404, "Match not found.", null);
            MatchState before = MatchEngine.Evaluate(ctx);

            if (!string.IsNullOrWhiteSpace(expectedVersion) && expectedVersion != before.Version)
                return new ActionOutcome(409, "The match has changed since you loaded it. Refresh and try again.", before);

            using (MySqlTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    ActionError? error = apply(conn, ctx, before);
                    if (error != null)
                    {
                        tx.Rollback();
                        return new ActionOutcome(error.Status, error.Message, before);
                    }

                    MatchContext after = Load(conn, matchId)!;
                    MatchState afterState = MatchEngine.Evaluate(after);

                    ActionError? bracketError = ReconcileBracket(conn, ctx, after, afterState);
                    if (bracketError != null)
                    {
                        tx.Rollback();
                        return new ActionOutcome(bracketError.Status, bracketError.Message, before);
                    }

                    PersistDerived(conn, after, afterState);
                    tx.Commit();
                }
                catch (MySqlException ex)
                {
                    tx.Rollback();
                    return new ActionOutcome(400, $"The change was rejected by the database: {ex.Message}", before);
                }
            }

            finalState = GetState(conn, matchId)!;
        }
        finally
        {
            matchLock.Release();
        }

        await Broadcast(connManager, finalState, actionName);
        return new ActionOutcome(200, null, finalState);
    }

    /// <summary>Sends the current state to websocket clients (used after changes made outside this pipeline, e.g. lineups).</summary>
    public static async Task BroadcastState(ConnectionManager connManager, ulong matchId, string actionName)
    {
        MatchState? state;
        using (MySqlConnection conn = connManager.IssueConnection())
            state = GetState(conn, matchId);
        if (state != null) await Broadcast(connManager, state, actionName);
    }

    private static async Task Broadcast(ConnectionManager connManager, MatchState state, string actionName)
    {
        await MatchWebSocketManager.BroadcastToMatch(state.MatchId, "match_state", new { action = actionName, state }, WebJson);

        // Legacy event, kept until clients move to match_state.
        DataTypes.Match legacy;
        using (MySqlConnection conn = connManager.IssueConnection())
            legacy = MatchTools.GetMatchById(conn, state.MatchId);
        await MatchWebSocketManager.BroadcastToMatch(state.MatchId, "match_updated", legacy);
    }

    // ── Derived fields ───────────────────────────────────────────────────────

    private static string StatusText(MatchStatus status) => status switch
    {
        MatchStatus.NotStarted => "Not Started",
        MatchStatus.InProgress => "In Progress",
        MatchStatus.Complete => "Complete",
        MatchStatus.Forfeit => "Forfeit",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static (ulong? winnerUid, int? winnerTeam) WinnerIds(MatchContext ctx, MatchState state)
    {
        DataTypes.Match m = ctx.Match;
        if (state.Status == MatchStatus.Forfeit) return (m.WinnerID, m.WinnerTeamID);
        if (state.Status != MatchStatus.Complete || state.WinnerSide is not int w) return (null, null);
        return ctx.IsTeamMatch
            ? (null, w == 1 ? m.Team1ID : m.Team2ID)
            : (w == 1 ? m.Player1ID : m.Player2ID, null);
    }

    private static void PersistDerived(MySqlConnection conn, MatchContext ctx, MatchState state)
    {
        (ulong? winnerUid, int? winnerTeam) = WinnerIds(ctx, state);
        MySqlCommand cmd = new MySqlCommand("""
            UPDATE matches SET
                Player1Points = @P1, Player2Points = @P2, MatchComplete = @Complete,
                WinnerUID = @WinnerUID, WinnerTeamID = @WinnerTeamID, Status = @Status,
                Player1ReplayUsed = @R1, Player2ReplayUsed = @R2, BanCount = @Bans, PickCount = @Picks
            WHERE MatchID = @MatchID
            """, conn);
        cmd.Parameters.AddWithValue("@P1", state.Sides[0].Points);
        cmd.Parameters.AddWithValue("@P2", state.Sides[1].Points);
        cmd.Parameters.AddWithValue("@Complete", state.Status is MatchStatus.Complete or MatchStatus.Forfeit);
        cmd.Parameters.AddWithValue("@WinnerUID", winnerUid ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@WinnerTeamID", winnerTeam ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", StatusText(state.Status));
        cmd.Parameters.AddWithValue("@R1", state.Sides[0].ReplayUsed);
        cmd.Parameters.AddWithValue("@R2", state.Sides[1].ReplayUsed);
        cmd.Parameters.AddWithValue("@Bans", state.Format.Bans ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Picks", state.Format.Picks ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchID", state.MatchId);
        cmd.ExecuteNonQuery();

        // Replay flags on scores are derived too. IDs are numbers from the database, so inlining them is safe.
        string replayIds = state.ReplayAttemptScoreIds.Count == 0 ? "-1" : string.Join(",", state.ReplayAttemptScoreIds);
        string calledAfterIds = state.ReplayCalledAfterScoreIds.Count == 0 ? "-1" : string.Join(",", state.ReplayCalledAfterScoreIds);
        MySqlCommand flags = new MySqlCommand(
            $"UPDATE player_scores SET IsReplayAttempt = ScoreID IN ({replayIds}), ReplayCalledAfter = ScoreID IN ({calledAfterIds}) WHERE MatchID = @MatchID",
            conn);
        flags.Parameters.AddWithValue("@MatchID", state.MatchId);
        flags.ExecuteNonQuery();
    }

    // ── Bracket ──────────────────────────────────────────────────────────────

    /// <summary>
    /// If a match's result changed, takes the previous winner/loser back out of their next matches (only if those
    /// haven't started) and advances the new result.
    /// </summary>
    private static ActionError? ReconcileBracket(MySqlConnection conn, MatchContext before, MatchContext after, MatchState afterState)
    {
        DataTypes.Match old = before.Match;
        bool wasComplete = old.Complete;
        bool isComplete = afterState.Status is MatchStatus.Complete or MatchStatus.Forfeit;
        (ulong? newWinnerUid, int? newWinnerTeam) = WinnerIds(after, afterState);
        bool winnerChanged = old.WinnerID != newWinnerUid || old.WinnerTeamID != newWinnerTeam;

        if (wasComplete && (!isComplete || winnerChanged))
        {
            bool team = before.IsTeamMatch;
            ulong? oldWinner = team ? (ulong?)old.WinnerTeamID : old.WinnerID;
            ulong? oldLoser = oldWinner == null ? null
                : team ? (ulong?)(old.WinnerTeamID == old.Team1ID ? old.Team2ID : old.Team1ID)
                : (old.WinnerID == old.Player1ID ? old.Player2ID : old.Player1ID);

            foreach ((ulong? target, ulong? participant) in new[] { (old.WinnerGoesToMatch, oldWinner), (old.LoserGoesToMatch, oldLoser) })
            {
                if (target == null || participant == null) continue;
                DataTypes.Match next = MatchTools.GetMatchById(conn, target.Value);
                if (next.MatchID == 0) continue;

                string? column = team
                    ? (next.Team1ID == (int?)participant ? "Team1ID" : next.Team2ID == (int?)participant ? "Team2ID" : null)
                    : (next.Player1ID == participant ? "Player1UID" : next.Player2ID == participant ? "Player2UID" : null);
                if (column == null) continue;

                if (next.PickBans.Count > 0 || next.Scores.Count > 0)
                    return new ActionError(409, $"This changes the result of match {old.MatchID}, but match {next.MatchID} that its result fed into has already started. Correct that match first.");

                MySqlCommand clear = new MySqlCommand($"UPDATE matches SET {column} = NULL WHERE MatchID = @MatchID", conn);
                clear.Parameters.AddWithValue("@MatchID", next.MatchID);
                clear.ExecuteNonQuery();
            }
        }

        if (isComplete && (!wasComplete || winnerChanged))
        {
            DataTypes.Match advanced = after.Match;
            advanced.Complete = true;
            advanced.WinnerID = newWinnerUid;
            advanced.WinnerTeamID = newWinnerTeam;
            MatchTools.AdvancePlayersFromMatch(conn, advanced);
        }
        return null;
    }

    // ── Guarded actions ──────────────────────────────────────────────────────

    private static ActionError NotNow(MatchState state, string what) =>
        new(409, $"Can't {what} now: the next action is '{JsonSerializer.Serialize(state.Next.Action).Trim('"')}'." +
                 (state.Next.Message != null ? $" {state.Next.Message}" : ""));

    private static (int? playerId, int? teamId) ActorFor(MatchContext ctx, int side) => ctx.IsTeamMatch
        ? (null, side == 1 ? ctx.Match.Team1ID : ctx.Match.Team2ID)
        : ((int?)(side == 1 ? ctx.Match.Player1ID : ctx.Match.Player2ID), null);

    /// <summary>Bans or picks <paramref name="mapId"/> for whichever side's turn it is.</summary>
    public static ActionError? PickBan(MySqlConnection conn, MatchContext ctx, MatchState state, ulong mapId)
    {
        if (state.Next.Action is not (NextActionType.Ban or NextActionType.Pick)) return NotNow(state, "ban or pick");
        PoolMapState? poolMap = state.Maps.FirstOrDefault(p => p.MapId == mapId);
        if (poolMap == null) return new ActionError(400, $"Map {mapId} is not in this match's pool.");
        if (poolMap.Status != PoolMapStatus.Available)
            return new ActionError(400, $"Map {mapId} has already been {poolMap.Status.ToString().ToLowerInvariant()}.");

        (int? playerId, int? teamId) = ActorFor(ctx, state.Next.Side!.Value);
        PickBanTools.CreatePickBan(conn, new CreatePickBanRequest
        {
            MatchID = ctx.Match.MatchID, IsPick = state.Next.Action == NextActionType.Pick,
            PlayerID = playerId, TeamID = teamId, MapID = mapId,
        });
        return null;
    }

    /// <summary>Records a player's score on the map being played. Re-submitting before the map is settled replaces it.</summary>
    public static ActionError? SubmitScore(MySqlConnection conn, MatchContext ctx, MatchState state, ulong userId, int score, int maxScore, int misses)
    {
        if (maxScore <= 0 || score < 0 || score > maxScore || misses < 0)
            return new ActionError(400, "Scores need 0 <= score <= maxScore, maxScore > 0 and misses >= 0.");
        if (state.Next.Action != NextActionType.SubmitScores) return NotNow(state, "submit scores");

        GameState game = state.Games.First(g => g.MapId == state.Next.MapId && g.Status != GameStatus.Settled);
        AttemptState attempt = game.Attempts.Last();
        Participant? player = ctx.Participants.FirstOrDefault(p => p.UserId == userId);
        if (player == null) return new ActionError(400, $"User {userId} is not playing in this match.");

        ScoreState? existing = attempt.Scores.FirstOrDefault(s => s.UserId == userId);
        if (existing != null)
        {
            ScoreTools.UpdateMatchScore(conn, existing.ScoreId, new UpdateMatchScoreRequest { Score = score, MaxScore = maxScore, Misses = misses });
            return null;
        }
        if (!state.Next.AwaitingScoresFrom.Contains(userId))
            return new ActionError(400, $"User {userId} is not playing map {game.MapId}.");
        if (player.Ssid == null)
            return new ActionError(400, $"User {userId} has no ScoreSaber ID linked, so their score can't be recorded.");

        ScoreTools.CreateMatchScore(conn, new FrontFacingMatchScore
        {
            UID = player.Ssid.Value, MapID = game.MapId, MatchID = ctx.Match.MatchID,
            Score = score, MaxScore = maxScore, Misses = misses,
            TeamID = ctx.IsTeamMatch ? ActorFor(ctx, player.Side).teamId : null,
        });
        return null;
    }

    /// <summary>The loser of the most recent map calls their replay. <paramref name="side"/> may be given to double-check who's calling.</summary>
    public static ActionError? CallReplay(MySqlConnection conn, MatchContext ctx, MatchState state, int? side)
    {
        if (state.ReplayWindow == null)
            return new ActionError(409, "No replay can be called now: replays are only available to the loser of a map, straight after it, once per match.");
        if (side.HasValue && side != state.ReplayWindow.Side)
            return new ActionError(400, $"Only side {state.ReplayWindow.Side}, who lost map {state.ReplayWindow.MapId}, can call a replay now.");

        string column = state.ReplayWindow.Side == 1 ? "Player1ReplayUsed" : "Player2ReplayUsed";
        SetMatchColumn(conn, ctx.Match.MatchID, column, true);
        return null;
    }

    /// <summary>Confirms the result of a deciding map when the loser chooses not to replay it.</summary>
    public static ActionError? ConfirmResult(MySqlConnection conn, MatchContext ctx, MatchState state)
    {
        if (state.Next.Action != NextActionType.ReplayOrConfirm) return NotNow(state, "confirm the result");
        SetMatchColumn(conn, ctx.Match.MatchID, "MatchComplete", true);
        return null;
    }

    /// <summary>Even pools: enters the secret tiebreaker map when the series is tied after every pick.</summary>
    public static ActionError? SetTiebreaker(MySqlConnection conn, MatchContext ctx, MatchState state, ulong mapId)
    {
        if (state.Next.Action != NextActionType.SetTiebreaker) return NotNow(state, "set the tiebreaker");
        if (MapTools.GetMapById(conn, mapId, includePrivate: true).MID == 0) return new ActionError(400, $"Map {mapId} does not exist.");
        if (state.Maps.Any(p => p.MapId == mapId && p.Status != PoolMapStatus.Available) || state.Games.Any(g => g.MapId == mapId))
            return new ActionError(400, $"Map {mapId} has already been used in this match.");

        PickBanTools.CreatePickBan(conn, new CreatePickBanRequest { MatchID = ctx.Match.MatchID, IsPick = true, MapID = mapId });
        return null;
    }

    public static ActionError? Forfeit(MySqlConnection conn, MatchContext ctx, MatchState state, int forfeitingSide)
    {
        if (forfeitingSide is not (1 or 2)) return new ActionError(400, "Side must be 1 or 2.");
        if (state.Status is MatchStatus.Complete or MatchStatus.Forfeit) return new ActionError(409, "The match is already finished.");
        DataTypes.Match m = ctx.Match;
        int winner = 3 - forfeitingSide;
        bool ready = ctx.IsTeamMatch ? m.Team1ID.HasValue && m.Team2ID.HasValue : m.Player1ID.HasValue && m.Player2ID.HasValue;
        if (!ready) return new ActionError(409, "Both sides must be set before a forfeit can be recorded.");

        (int? playerId, int? teamId) = ActorFor(ctx, winner);
        MySqlCommand cmd = new MySqlCommand(
            "UPDATE matches SET Status = 'Forfeit', MatchComplete = 1, WinnerUID = @WinnerUID, WinnerTeamID = @WinnerTeamID WHERE MatchID = @MatchID",
            conn);
        cmd.Parameters.AddWithValue("@WinnerUID", playerId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@WinnerTeamID", teamId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchID", m.MatchID);
        cmd.ExecuteNonQuery();
        return null;
    }

    /// <summary>
    /// Takes back the most recent action, one record at a time:
    /// a forfeit → a confirmed result → an unplayed replay call → the latest score on the latest map → the latest pick/ban.
    /// </summary>
    public static ActionError? Undo(MySqlConnection conn, MatchContext ctx, MatchState state)
    {
        DataTypes.Match m = ctx.Match;
        if (state.Status == MatchStatus.Forfeit)
        {
            MySqlCommand cmd = new MySqlCommand(
                "UPDATE matches SET Status = NULL, MatchComplete = 0, WinnerUID = NULL, WinnerTeamID = NULL WHERE MatchID = @MatchID", conn);
            cmd.Parameters.AddWithValue("@MatchID", m.MatchID);
            cmd.ExecuteNonQuery();
            return null;
        }

        if (state.Status == MatchStatus.Complete && m.Complete)
        {
            DataTypes.Match unconfirmed = m;
            unconfirmed.Complete = false;
            MatchState wouldBe = MatchEngine.Evaluate(new MatchContext { Match = unconfirmed, Pool = ctx.Pool, TeamActivePlayers = ctx.TeamActivePlayers });
            if (wouldBe.Next.Action == NextActionType.ReplayOrConfirm)
            {
                SetMatchColumn(conn, m.MatchID, "MatchComplete", false);
                return null;
            }
        }

        GameState? last = state.Games.LastOrDefault();
        if (last is { Status: GameStatus.Replaying } && last.Attempts.Last().Scores.Count == 0 && last.Attempts.Last().CalledBySide is int caller)
        {
            SetMatchColumn(conn, m.MatchID, caller == 1 ? "Player1ReplayUsed" : "Player2ReplayUsed", false);
            return null;
        }

        if (last != null && state.LastGameIsLatestAction)
        {
            MatchScore? latest = m.Scores.Where(s => s.MapID == last.MapId).OrderByDescending(s => s.ScoreID).Cast<MatchScore?>().FirstOrDefault();
            if (latest.HasValue)
            {
                ScoreTools.DeleteMatchScore(conn, latest.Value.ScoreID);
                return null;
            }
            if (last.IsTiebreaker && last.PickBanId is int tiebreakerRow)
            {
                PickBanTools.DeletePickBan(conn, tiebreakerRow);
                return null;
            }
        }

        PickBan? lastRow = m.PickBans.Where(r => ctx.SideOf(r) > 0).Cast<PickBan?>().LastOrDefault();
        if (lastRow.HasValue)
        {
            PickBanTools.DeletePickBan(conn, lastRow.Value.PickBanID);
            return null;
        }
        return new ActionError(409, "There is nothing to undo.");
    }

    // ── Overrides: edit records out of turn; results are still recomputed ───

    public static ActionError? OverrideAddPickBan(MySqlConnection conn, MatchContext ctx, MatchState state, CreatePickBanRequest request)
    {
        int side = ctx.SideOf(new PickBan { PlayerID = request.PlayerID ?? 0, TeamID = request.TeamID });
        if (side <= 0) return new ActionError(400, "PlayerID/TeamID must be one of this match's sides.");
        PoolMapState? poolMap = state.Maps.FirstOrDefault(p => p.MapId == request.MapID);
        if (poolMap == null) return new ActionError(400, $"Map {request.MapID} is not in this match's pool.");
        if (poolMap.Status != PoolMapStatus.Available)
            return new ActionError(400, $"Map {request.MapID} has already been {poolMap.Status.ToString().ToLowerInvariant()}.");
        PickBanTools.CreatePickBan(conn, request);
        return null;
    }

    public static ActionError? OverrideDeletePickBan(MySqlConnection conn, MatchContext ctx, int pickBanId)
    {
        if (ctx.Match.PickBans.All(r => r.PickBanID != pickBanId)) return new ActionError(404, "Pick/ban not found in this match.");
        PickBanTools.DeletePickBan(conn, pickBanId);
        return null;
    }

    public static ActionError? OverrideAddScore(MySqlConnection conn, MatchContext ctx, MatchState state, FrontFacingMatchScore score)
    {
        if (score.MaxScore <= 0 || score.Score < 0 || score.Score > score.MaxScore || score.Misses < 0)
            return new ActionError(400, "Scores need 0 <= score <= maxScore, maxScore > 0 and misses >= 0.");
        Participant? player = ctx.Participants.FirstOrDefault(p => p.Ssid == score.UID);
        if (player == null) return new ActionError(400, $"ScoreSaber ID {score.UID} doesn't belong to anyone in this match.");
        if (state.Games.All(g => g.MapId != score.MapID)) return new ActionError(400, $"Map {score.MapID} hasn't been played in this match.");

        score.TeamID = ctx.IsTeamMatch ? ActorFor(ctx, player.Side).teamId : null;
        score.IsReplayAttempt = false; // derived by the engine
        ScoreTools.CreateMatchScore(conn, score);
        return null;
    }

    public static ActionError? OverrideUpdateScore(MySqlConnection conn, MatchContext ctx, ulong scoreId, UpdateMatchScoreRequest request)
    {
        if (request.MaxScore <= 0 || request.Score < 0 || request.Score > request.MaxScore || request.Misses < 0)
            return new ActionError(400, "Scores need 0 <= score <= maxScore, maxScore > 0 and misses >= 0.");
        if (ctx.Match.Scores.All(s => s.ScoreID != scoreId)) return new ActionError(404, "Score not found in this match.");
        ScoreTools.UpdateMatchScore(conn, scoreId, request);
        return null;
    }

    public static ActionError? OverrideDeleteScore(MySqlConnection conn, MatchContext ctx, ulong scoreId)
    {
        if (ctx.Match.Scores.All(s => s.ScoreID != scoreId)) return new ActionError(404, "Score not found in this match.");
        ScoreTools.DeleteMatchScore(conn, scoreId);
        return null;
    }

    /// <summary>
    /// Edits match details. Results are ignored (they come from the records); format and participants
    /// can only change before play starts.
    /// </summary>
    public static ActionError? UpdateMetadata(MySqlConnection conn, MatchContext ctx, MatchState state, DataTypes.Match update)
    {
        DataTypes.Match m = ctx.Match;
        bool started = state.Status != MatchStatus.NotStarted;
        bool structural = update.BestOf != m.BestOf || update.MapPoolID != m.MapPoolID || update.Season != m.Season
                          || update.Player1ID != m.Player1ID || update.Player2ID != m.Player2ID
                          || update.Team1ID != m.Team1ID || update.Team2ID != m.Team2ID;
        if (started && structural)
            return new ActionError(409, "BestOf, map pool, season and participants can't change once the match has started. Undo its actions first.");
        MatchTools.UpdateMatchMetadata(conn, update);
        return null;
    }

    private static void SetMatchColumn(MySqlConnection conn, ulong matchId, string column, bool value)
    {
        MySqlCommand cmd = new MySqlCommand($"UPDATE matches SET {column} = @Value WHERE MatchID = @MatchID", conn);
        cmd.Parameters.AddWithValue("@Value", value);
        cmd.Parameters.AddWithValue("@MatchID", matchId);
        cmd.ExecuteNonQuery();
    }
}
