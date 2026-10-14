using System.Security.Cryptography;
using System.Text;
using FishyAPI.Tools.DataTypes;

namespace FishyAPI.Tools.MatchFlow;

public sealed record Participant(int Side, ulong UserId, ulong? Ssid);

/// <summary>Everything the engine needs about one match, loaded from the database.</summary>
public sealed class MatchContext
{
    public required DataTypes.Match Match { get; init; }
    public required List<Map> Pool { get; init; }
    public int? TeamActivePlayers { get; init; }

    public bool IsTeamMatch => Match.Team1ID.HasValue || Match.Team2ID.HasValue;

    /// <summary>Solo: each side's player. Team: every member of each team.</summary>
    public List<Participant> Participants
    {
        get
        {
            List<Participant> result = new();
            if (IsTeamMatch)
            {
                foreach (var (side, team) in new[] { (1, Match.Team1), (2, Match.Team2) })
                    if (team.HasValue)
                        result.AddRange(team.Value.Members.Select(m => new Participant(side, m.UserID, m.User?.SSID)));
            }
            else
            {
                if (Match.Player1ID.HasValue) result.Add(new Participant(1, Match.Player1ID.Value, Match.Player1User?.SSID));
                if (Match.Player2ID.HasValue) result.Add(new Participant(2, Match.Player2ID.Value, Match.Player2User?.SSID));
            }
            return result;
        }
    }

    /// <summary>1 or 2 for a pick/ban row's acting side, 0 for a side-less row (secret tiebreaker), -1 if it names a non-participant.</summary>
    public int SideOf(PickBan row)
    {
        if (IsTeamMatch)
        {
            if (row.TeamID == null) return row.PlayerID == 0 ? 0 : -1;
            return row.TeamID == Match.Team1ID ? 1 : row.TeamID == Match.Team2ID ? 2 : -1;
        }
        if (row.PlayerID == 0 && row.TeamID == null) return 0;
        return (ulong)row.PlayerID == Match.Player1ID ? 1 : (ulong)row.PlayerID == Match.Player2ID ? 2 : -1;
    }
}

/// <summary>
/// Pure function from a match's recorded data (picks/bans, scores, lineups, replay calls) to its full state.
/// Implements the rules in the match-rules memory / plan; nothing here touches the database.
/// </summary>
public static class MatchEngine
{
    public static MatchState Evaluate(MatchContext ctx)
    {
        MatchState first = Walk(ctx, forceReplayOnGame: -1);

        // A replay that has been called (flag set) but not yet played applies to the most recent map.
        int? pendingReplaySide = PendingReplaySide(ctx, first);
        MatchState state = pendingReplaySide.HasValue ? Walk(ctx, forceReplayOnGame: first.Games.Count - 1) : first;

        // Matches recorded before the guarded flow may have results that their data doesn't support.
        DataTypes.Match m = ctx.Match;
        int? storedWinner = StoredWinnerSide(ctx);
        if (m.Complete && state.Status != MatchStatus.Forfeit &&
            (state.Sides[0].Points != (m.Player1Score ?? 0) || state.Sides[1].Points != (m.Player2Score ?? 0) || state.WinnerSide != storedWinner))
        {
            state.StoredResultDiffers = true;
            state.Deviations.Add($"Stored result {m.Player1Score ?? 0}-{m.Player2Score ?? 0} differs from the result recomputed from this match's records " +
                                 $"({state.Sides[0].Points}-{state.Sides[1].Points}). It is only rewritten if this match is changed.");
        }
        return state;
    }

    public static string ComputeVersion(DataTypes.Match m)
    {
        StringBuilder sb = new();
        sb.Append(m.MatchID).Append('|').Append(m.BestOf).Append('|').Append(m.MapPoolID).Append('|')
          .Append(m.Player1ID).Append(',').Append(m.Player2ID).Append(',').Append(m.Team1ID).Append(',').Append(m.Team2ID).Append('|')
          .Append(m.Complete).Append(m.Status).Append(m.Player1ReplayUsed).Append(m.Player2ReplayUsed).Append(m.WinnerID).Append(m.WinnerTeamID).Append('|');
        foreach (PickBan pb in m.PickBans) sb.Append(pb.PickBanID).Append(':').Append(pb.MapID).Append(pb.IsPick ? 'p' : 'b').Append(';');
        sb.Append('|');
        foreach (MatchScore s in m.Scores) sb.Append(s.ScoreID).Append(':').Append(s.Score).Append('/').Append(s.MaxScore).Append('/').Append(s.Misses).Append(';');
        sb.Append('|');
        foreach (MatchLineup l in m.Lineups) sb.Append(l.LineupID).Append(';');
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
    }

    // ── Core walk ────────────────────────────────────────────────────────────

    private static MatchState Walk(MatchContext ctx, int forceReplayOnGame)
    {
        DataTypes.Match m = ctx.Match;
        MatchState state = new()
        {
            MatchId = m.MatchID,
            Version = ComputeVersion(m),
            BestOf = m.BestOf,
            IsTeamMatch = ctx.IsTeamMatch,
            FinalizedInput = m.Complete && !IsForfeit(m),
        };

        // Format
        SequencePlan? plan = null;
        state.Format.PoolSize = ctx.Pool.Count;
        if (m.BestOf is int bestOf)
        {
            (plan, string? problem) = MatchSequence.Build(bestOf, ctx.Pool.Count);
            state.Format.Problem = problem;
        }
        else state.Format.Problem = "BestOf is not set for this match.";
        if (plan != null)
        {
            state.Format.Supported = true;
            state.Format.Bans = plan.Bans;
            state.Format.Picks = plan.Picks;
            state.Format.TiebreakerSource = plan.TiebreakerFromPool ? "pool" : "secret";
            state.WinsNeeded = plan.WinsNeeded;
        }
        int winsNeeded = plan?.WinsNeeded ?? ((m.BestOf ?? 1) + 1) / 2;

        // Sides
        List<Participant> participants = ctx.Participants;
        for (int side = 1; side <= 2; side++)
        {
            state.Sides.Add(new SideState
            {
                Side = side,
                UserId = ctx.IsTeamMatch ? null : (side == 1 ? m.Player1ID : m.Player2ID),
                TeamId = ctx.IsTeamMatch ? (side == 1 ? m.Team1ID : m.Team2ID) : null,
                Name = ctx.IsTeamMatch
                    ? (side == 1 ? m.Team1?.TeamName : m.Team2?.TeamName)
                    : (side == 1 ? m.Player1User?.Username : m.Player2User?.Username),
            });
        }

        // Pool view
        Dictionary<ulong, PoolMapState> poolMaps = ctx.Pool.ToDictionary(p => p.MID, p => new PoolMapState { MapId = p.MID, Map = p, Status = PoolMapStatus.Available });
        Dictionary<ulong, Map> knownMaps = ctx.Pool.ToDictionary(p => p.MID);
        foreach (PickBan pb in m.PickBans)
            if (pb.MapID.HasValue && pb.Map.HasValue) knownMaps.TryAdd(pb.MapID.Value, pb.Map.Value);

        // Scores per map per ScoreSaber ID, in submission order
        Dictionary<ulong, Dictionary<ulong, List<MatchScore>>> scores = new();
        foreach (MatchScore s in m.Scores.OrderBy(s => s.ScoreID))
        {
            if (!scores.TryGetValue(s.MapID, out var byPlayer)) scores[s.MapID] = byPlayer = new();
            if (!byPlayer.TryGetValue(s.UID, out var list)) byPlayer[s.UID] = list = new();
            list.Add(s);
        }

        int[] points = new int[3];
        int? decidedBy = null;
        GameState? pending = null;
        int stepIndex = 0;
        PickBan? secretTiebreaker = null;
        int lastSideRowGame = -1; // index in Games of the game picked by the last side row, if that row was a pick

        foreach (PickBan row in m.PickBans)
        {
            int side = ctx.SideOf(row);
            if (row.MapID == null) { state.Deviations.Add($"Pick/ban {row.PickBanID} has no map."); continue; }
            ulong mapId = row.MapID.Value;

            if (side == 0)
            {
                if (row.IsPick && secretTiebreaker == null) secretTiebreaker = row;
                else state.Deviations.Add($"Pick/ban {row.PickBanID} has no side and isn't a tiebreaker.");
                continue;
            }
            if (side < 0) { state.Deviations.Add($"Pick/ban {row.PickBanID} was made by someone not in this match."); continue; }

            SequenceStep? step = plan != null && stepIndex < plan.Steps.Count ? plan.Steps[stepIndex] : null;
            StepAction action = row.IsPick ? StepAction.Pick : StepAction.Ban;
            if (step == null) state.Deviations.Add($"Step {stepIndex + 1}: {Describe(action, side)} is beyond the expected sequence.");
            else if (step.Action != action || step.Side != side)
                state.Deviations.Add($"Step {stepIndex + 1}: expected {Describe(step.Action, step.Side)}, recorded {Describe(action, side)}.");
            if (decidedBy.HasValue) state.Deviations.Add($"Step {stepIndex + 1} was recorded after the match was decided.");
            if (pending != null) state.Deviations.Add($"Step {stepIndex + 1} was recorded before map {pending.MapId} was finished.");
            stepIndex++;
            lastSideRowGame = -1;

            if (!poolMaps.TryGetValue(mapId, out PoolMapState? poolMap))
                state.Deviations.Add($"Map {mapId} ({Describe(action, side)}) is not in this match's pool.");
            else if (poolMap.Status != PoolMapStatus.Available)
                state.Deviations.Add($"Map {mapId} was {Describe(action, side)} after already being {poolMap.Status.ToString().ToLowerInvariant()}.");

            if (poolMap != null)
            {
                poolMap.Status = row.IsPick ? PoolMapStatus.Picked : PoolMapStatus.Banned;
                poolMap.Side = side;
            }
            if (!row.IsPick) continue;

            GameState game = EvaluateGame(ctx, state, mapId, knownMaps, scores, isTiebreaker: false,
                force: state.Games.Count == forceReplayOnGame);
            game.PickedBySide = side;
            game.PickBanId = row.PickBanID;
            AddGame(state, game, poolMap, points, winsNeeded, ref decidedBy, ref pending);
            lastSideRowGame = state.Games.Count - 1;
        }

        // Sequence view
        if (plan != null)
        {
            List<PickBan> sideRows = m.PickBans.Where(r => ctx.SideOf(r) > 0 && r.MapID.HasValue).ToList();
            foreach (SequenceStep step in plan.Steps)
            {
                PickBan? done = step.Index < sideRows.Count ? sideRows[step.Index] : null;
                state.Sequence.Add(new SequenceStepState
                {
                    Step = step.Index + 1, Cycle = step.Cycle, Action = step.Action, Side = step.Side,
                    Done = done != null, MapId = done?.MapID, PickBanId = done?.PickBanID,
                });
            }
        }

        // Tiebreaker: after every pick, if the series is still level.
        bool needTiebreaker = plan != null && !decidedBy.HasValue && pending == null && stepIndex >= plan.Steps.Count;
        bool tiebreakerMissing = false;
        if (needTiebreaker)
        {
            ulong? tbMap = null;
            if (plan!.TiebreakerFromPool)
            {
                List<PoolMapState> left = poolMaps.Values.Where(p => p.Status == PoolMapStatus.Available).ToList();
                if (left.Count == 1) tbMap = left[0].MapId;
                else state.Deviations.Add($"Expected exactly one map left for the tiebreaker, found {left.Count}.");
            }
            else if (secretTiebreaker != null) tbMap = secretTiebreaker.Value.MapID;
            else tiebreakerMissing = true;

            if (tbMap.HasValue)
            {
                poolMaps.TryGetValue(tbMap.Value, out PoolMapState? tbPool);
                if (tbPool != null) tbPool.Status = PoolMapStatus.Tiebreaker;
                GameState game = EvaluateGame(ctx, state, tbMap.Value, knownMaps, scores, isTiebreaker: true,
                    force: state.Games.Count == forceReplayOnGame);
                game.PickBanId = secretTiebreaker?.PickBanID;
                AddGame(state, game, tbPool, points, winsNeeded, ref decidedBy, ref pending);
                lastSideRowGame = state.Games.Count - 1;
            }
        }
        else if (secretTiebreaker != null && plan != null && !plan.TiebreakerFromPool)
            state.Deviations.Add("A secret tiebreaker was entered before it was needed.");

        foreach (ulong mapId in scores.Keys)
            if (state.Games.All(g => g.MapId != mapId))
                state.Deviations.Add($"Scores were recorded for map {mapId}, which wasn't played in this match.");

        // Replays used: called on a map, or called and still to be played.
        bool[] replayUsed = new bool[3];
        foreach (GameState g in state.Games)
            foreach (int caller in g.ReplayCallers) replayUsed[caller] = true;
        if (m.Player1ReplayUsed) replayUsed[1] = true;
        if (m.Player2ReplayUsed) replayUsed[2] = true;

        // Results
        for (int side = 1; side <= 2; side++)
        {
            SideState s = state.Sides[side - 1];
            s.Points = points[side];
            s.ReplayUsed = replayUsed[side];
            s.ReplayAvailable = !replayUsed[side];
        }

        GameState? lastGame = state.Games.LastOrDefault();
        bool lastGameIsLatestAction = lastGame != null && state.Games.Count - 1 == lastSideRowGame;
        state.LastGameIsLatestAction = lastGameIsLatestAction;
        int? loserOfLast = lastGame is { Status: GameStatus.Settled, WinnerSide: int w } ? 3 - w : null;
        bool windowOpen = lastGameIsLatestAction && loserOfLast.HasValue && !replayUsed[loserOfLast.Value];

        if (IsForfeit(m))
        {
            state.Status = MatchStatus.Forfeit;
            state.WinnerSide = StoredWinnerSide(ctx);
            // A forfeit scores -1 for the side that forfeited and 0 for the winner, whatever was played before it.
            if (state.WinnerSide is int forfeitWinner)
            {
                state.Sides[forfeitWinner - 1].Points = 0;
                state.Sides[2 - forfeitWinner].Points = -1;
            }
            state.Next = new NextAction { Action = NextActionType.None, Message = "Match was forfeited." };
        }
        else if (decidedBy.HasValue)
        {
            bool waitForReplay = windowOpen && !state.FinalizedInput;
            if (waitForReplay)
            {
                state.Status = MatchStatus.InProgress;
                state.ReplayWindow = new ReplayWindow { Side = loserOfLast!.Value, MapId = lastGame!.MapId, IsDecidingMap = true };
                state.Next = new NextAction
                {
                    Action = NextActionType.ReplayOrConfirm, Side = loserOfLast, MapId = lastGame.MapId,
                    Message = $"Side {loserOfLast} lost the deciding map and can still call their replay. Call it, or confirm the result.",
                };
            }
            else
            {
                state.Status = MatchStatus.Complete;
                state.WinnerSide = decidedBy;
                state.Next = new NextAction { Action = NextActionType.None, Message = "Match complete." };
            }
        }
        else
        {
            state.Status = m.PickBans.Count == 0 && m.Scores.Count == 0 ? MatchStatus.NotStarted : MatchStatus.InProgress;
            if (windowOpen)
                state.ReplayWindow = new ReplayWindow { Side = loserOfLast!.Value, MapId = lastGame!.MapId, IsDecidingMap = false };

            bool sidesReady = ctx.IsTeamMatch ? m.Team1ID.HasValue && m.Team2ID.HasValue : m.Player1ID.HasValue && m.Player2ID.HasValue;
            if (plan == null)
                state.Next = new NextAction { Action = NextActionType.None, Message = state.Format.Problem };
            else if (!sidesReady)
                state.Next = new NextAction { Action = NextActionType.None, Message = "Both sides must be set before the match can be played." };
            else if (pending != null)
                state.Next = pending.Status == GameStatus.AwaitingLineup
                    ? new NextAction
                    {
                        Action = NextActionType.SetLineup, MapId = pending.MapId, AwaitingLineupFromSides = pending.AwaitingLineupFromSides,
                        Message = "Set the lineup for this map before entering scores.",
                    }
                    : new NextAction
                    {
                        Action = NextActionType.SubmitScores, MapId = pending.MapId,
                        Attempt = pending.Attempts.Last().Number, AttemptKind = pending.Attempts.Last().Kind,
                        AwaitingScoresFrom = pending.AwaitingScoresFrom,
                    };
            else if (stepIndex < plan.Steps.Count)
            {
                SequenceStep step = plan.Steps[stepIndex];
                state.Next = new NextAction { Action = step.Action == StepAction.Ban ? NextActionType.Ban : NextActionType.Pick, Side = step.Side, Step = step.Index + 1 };
            }
            else if (tiebreakerMissing)
                state.Next = new NextAction { Action = NextActionType.SetTiebreaker, Message = "Series is tied: enter the secret tiebreaker map." };
            else
                state.Next = new NextAction { Action = NextActionType.None, Message = "No further action is possible; check the deviations." };
        }

        foreach (SideState s in state.Sides) s.IsWinner = state.WinnerSide == s.Side;
        state.Maps = poolMaps.Values.ToList();
        return state;
    }

    private static void AddGame(MatchState state, GameState game, PoolMapState? poolMap, int[] points, int winsNeeded,
        ref int? decidedBy, ref GameState? pending)
    {
        game.Order = state.Games.Count + 1;
        if (poolMap != null) poolMap.PlayOrder = game.Order;
        state.Games.Add(game);

        if (game.Status != GameStatus.Settled)
        {
            pending ??= game;
            return;
        }

        // Maps recorded after the match was already decided (older data only) don't count.
        if (decidedBy.HasValue) return;
        points[game.WinnerSide!.Value]++;
        if (points[game.WinnerSide.Value] >= winsNeeded)
            decidedBy = game.WinnerSide;
    }

    // ── One map ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Settles one map from its scores. Attempt n for a player is their nth score on the map.
    ///   - An exact tie requires a fresh attempt; only that attempt counts (tie replay).
    ///   - Another attempt after a decided one is a replay called by the loser: each player's best of the two counts.
    ///   - <paramref name="force"/> marks a replay that has been called but not yet played on this map.
    /// </summary>
    private static GameState EvaluateGame(MatchContext ctx, MatchState state, ulong mapId, Dictionary<ulong, Map> knownMaps,
        Dictionary<ulong, Dictionary<ulong, List<MatchScore>>> scores, bool isTiebreaker, bool force)
    {
        GameState game = new() { MapId = mapId, IsTiebreaker = isTiebreaker };
        if (knownMaps.TryGetValue(mapId, out Map map)) game.Map = map;

        List<Participant>? required = RequiredPlayers(ctx, mapId, game);
        if (required == null)
        {
            game.Status = GameStatus.AwaitingLineup;
            return game;
        }
        foreach (Participant p in required.Where(p => p.Ssid == null))
            state.Deviations.Add($"User {p.UserId} has no ScoreSaber ID linked, so their scores can't be recorded.");

        scores.TryGetValue(mapId, out var byPlayer);
        List<MatchScore> ScoresOf(Participant p) =>
            p.Ssid.HasValue && byPlayer != null && byPlayer.TryGetValue(p.Ssid.Value, out var list) ? list : new List<MatchScore>();

        if (byPlayer != null)
            foreach (ulong ssid in byPlayer.Keys)
                if (required.All(p => p.Ssid != ssid))
                    state.Deviations.Add($"Map {mapId}: scores from ScoreSaber ID {ssid}, who isn't playing this map, were ignored.");

        int k = 1;
        AttemptKind kind = AttemptKind.Initial;
        int? calledBy = null;
        while (true)
        {
            AttemptState attempt = new() { Number = k, Kind = kind, CalledBySide = calledBy };
            foreach (Participant p in required)
            {
                List<MatchScore> list = ScoresOf(p);
                if (list.Count >= k) attempt.Scores.Add(ToState(list[k - 1], p));
            }
            attempt.Complete = required.All(p => ScoresOf(p).Count >= k);
            game.Attempts.Add(attempt);

            if (!attempt.Complete)
            {
                game.Status = kind == AttemptKind.Replay ? GameStatus.Replaying : GameStatus.AwaitingScores;
                game.AwaitingScoresFrom = required.Where(p => ScoresOf(p).Count < k).Select(p => p.UserId).ToList();
                return game;
            }

            long[] totals = new long[3];
            foreach (Participant p in required)
            {
                List<MatchScore> list = ScoresOf(p);
                ScoreState current = attempt.Scores.First(s => s.ScoreId == list[k - 1].ScoreID);
                int value = list[k - 1].Score;
                // On a replay, each player's better of their two attempts counts.
                if (kind == AttemptKind.Replay && list[k - 2].Score > value) value = list[k - 2].Score;
                else current.Counted = true;
                totals[p.Side] += value;
            }
            attempt.Side1Total = totals[1];
            attempt.Side2Total = totals[2];

            if (totals[1] == totals[2])
            {
                k++; kind = AttemptKind.TieReplay; calledBy = null;
                continue;
            }

            int winner = totals[1] > totals[2] ? 1 : 2;
            bool anotherAttemptRecorded = required.Any(p => ScoresOf(p).Count > k);
            if (anotherAttemptRecorded || force)
            {
                force = false;
                int loser = 3 - winner;
                foreach (Participant p in required)
                {
                    List<MatchScore> list = ScoresOf(p);
                    state.ReplayCalledAfterScoreIds.Add(list[k - 1].ScoreID);
                    if (list.Count > k) state.ReplayAttemptScoreIds.Add(list[k].ScoreID);
                }
                game.ReplayCallers.Add(loser);
                k++; kind = AttemptKind.Replay; calledBy = loser;
                continue;
            }

            game.WinnerSide = winner;
            game.Status = GameStatus.Settled;
            return game;
        }
    }

    /// <summary>Solo: both players. Team: each side's lineup for this map (null if a lineup isn't set yet).</summary>
    private static List<Participant>? RequiredPlayers(MatchContext ctx, ulong mapId, GameState game)
    {
        List<Participant> participants = ctx.Participants;
        if (!ctx.IsTeamMatch)
            return participants;

        List<Participant> required = new();
        for (int side = 1; side <= 2; side++)
        {
            int? teamId = side == 1 ? ctx.Match.Team1ID : ctx.Match.Team2ID;
            List<ulong> lineup = ctx.Match.Lineups.Where(l => l.MapID == mapId && l.TeamID == teamId).Select(l => l.UserID).ToList();
            bool complete = ctx.TeamActivePlayers is int n ? lineup.Count == n : lineup.Count > 0;
            if (!complete) game.AwaitingLineupFromSides.Add(side);
            required.AddRange(lineup.Select(uid => participants.FirstOrDefault(p => p.UserId == uid) ?? new Participant(side, uid, null)));
        }
        return game.AwaitingLineupFromSides.Count > 0 ? null : required;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int? PendingReplaySide(MatchContext ctx, MatchState state)
    {
        GameState? last = state.Games.LastOrDefault();
        foreach (int side in new[] { 1, 2 })
        {
            bool flagged = side == 1 ? ctx.Match.Player1ReplayUsed : ctx.Match.Player2ReplayUsed;
            if (!flagged || state.Games.Any(g => g.ReplayCallers.Contains(side))) continue;

            // The flag can also be left over from a replay on a map that a later correction means is no longer played
            // (its scores remain, with repeat attempts). That replay was used; it isn't a new, pending call.
            HashSet<ulong?> sideSsids = ctx.Participants.Where(p => p.Side == side).Select(p => p.Ssid).ToHashSet();
            bool usedOnUnplayedMap = ctx.Match.Scores
                .Where(s => sideSsids.Contains(s.UID) && state.Games.All(g => g.MapId != s.MapID))
                .GroupBy(s => (s.MapID, s.UID))
                .Any(g => g.Count() > 1);
            if (usedOnUnplayedMap)
            {
                state.Deviations.Add($"Side {side}'s replay was used on a map that is no longer played in this match.");
                continue;
            }

            // Called right after losing the most recent map, with nothing done since: the replay is still to be played.
            if (last is { Status: GameStatus.Settled } && last.WinnerSide == 3 - side && state.LastGameIsLatestAction)
                return side;
            state.Deviations.Add($"Side {side} is marked as having used their replay, but no replay was played.");
        }
        return null;
    }

    private static bool IsForfeit(DataTypes.Match m) => string.Equals(m.Status, "Forfeit", StringComparison.OrdinalIgnoreCase);

    private static int? StoredWinnerSide(MatchContext ctx)
    {
        DataTypes.Match m = ctx.Match;
        if (ctx.IsTeamMatch)
            return m.WinnerTeamID == null ? null : m.WinnerTeamID == m.Team1ID ? 1 : m.WinnerTeamID == m.Team2ID ? 2 : null;
        return m.WinnerID == null ? null : m.WinnerID == m.Player1ID ? 1 : m.WinnerID == m.Player2ID ? 2 : null;
    }

    private static ScoreState ToState(MatchScore s, Participant p) => new()
    {
        ScoreId = s.ScoreID, UserId = p.UserId, ScoresaberId = s.UID, Side = p.Side,
        Score = s.Score, MaxScore = s.MaxScore, Misses = s.Misses,
    };

    private static string Describe(StepAction action, int side) => $"{(action == StepAction.Ban ? "ban" : "pick")} by side {side}";
}
