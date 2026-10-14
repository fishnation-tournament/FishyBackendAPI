// ─── Supporting Types ────────────────────────────────────────────────────────

export interface FrontFacingUser {
    UID:               number;
    SSID:              string | undefined;   // serialised as string (ulong)
    OptBLUID:          string | undefined;   // serialised as string (ulong)
    Username:          string;
    UserPfpLink:       string | undefined;
    UserBio:           string | undefined;
    DiscordID:         string | undefined;   // serialised as string (ulong)
    DiscordUsername:   string | undefined;
    RegistrationDate:  string;               // ISO 8601
    Roles:             string[];             // role keys, e.g. ["user", "organizer"]
    Title:             string | undefined;   // display text, not used for access
    ScoreSaberProfile: object | undefined;   // raw JSON blob
    BeatLeaderProfile: object | undefined;   // raw JSON blob
}

export interface MatchScore {
    ScoreID:         number;
    UID:             number;
    MapID:           number;
    MatchID:         number;
    Score:           number;
    MaxScore:        number;
    Misses:          number;
    /** A replay was called after this score. The score is still a required benchmark for the replaying player. */
    ReplayCalledAfter: boolean;
    IsReplayAttempt:   boolean;
}

export interface FrontFacingMatchScore {
    UID:             number;
    MapID:           number;
    MatchID:         number;
    Score:           number;
    MaxScore:        number;
    Misses:          number;
    IsReplayAttempt: boolean;
}

export interface PickBan {
    PickBanID: number;
    MatchID:   number;
    IsPick:    boolean;   // true = pick, false = ban
    PlayerID:  number;
}

export interface Match {
    MatchID:            number;
    Player1ID:          number | undefined;
    Player2ID:          number | undefined;
    Complete:           boolean;
    WinnerID:           number | undefined;
    Player1Score:       number | undefined;
    Player2Score:       number | undefined;
    MatchDate:          string | undefined;  // ISO 8601
    Scores:             MatchScore[];
    Season:             number;
    MapPoolID:          number;
    Status:             string | undefined;
    BestOf:             number | undefined;
    Player1ReplayUsed:  boolean;
    Player2ReplayUsed:  boolean;
    IsStreamed:         boolean;
    CoordinatorUID:     number | undefined;
    Player1User:        FrontFacingUser | undefined;
    Player2User:        FrontFacingUser | undefined;
    CoordinatorUser:    FrontFacingUser | undefined;
    RoundName:          string | undefined;
    RoundOrder:         number | undefined;
    BanCount:           number | undefined;
    PickCount:          number | undefined;
}

export interface ReplayCalledData {
    callingPlayerUID: number;
    /** Scores from the attempt on which the replay was called. The replaying player must beat all of these. */
    preReplayScores:  MatchScore[];
    match:            Match;
}

// ─── Individual Event Types ───────────────────────────────────────────────────

export interface ScorePostedEvent {
    event:   "score_posted";
    matchId: number;
    data:    FrontFacingMatchScore;
}

export interface ScoreUpdatedEvent {
    event:   "score_updated";
    matchId: number;
    data:    MatchScore;
}

export interface PickAddedEvent {
    event:   "pick_added";
    matchId: number;
    data:    PickBan;
}

export interface BanAddedEvent {
    event:   "ban_added";
    matchId: number;
    data:    PickBan;
}

export interface PickBanRemovedEvent {
    event:   "pick_ban_removed";
    matchId: number;
    data: {
        pickBanId: number;
    };
}

export interface MatchUpdatedEvent {
    event:   "match_updated";
    matchId: number;
    data:    Match;
}

export interface ReplayCalledEvent {
    event:   "replay_called";
    matchId: number;
    data:    ReplayCalledData;
}

// ─── Server-computed match state (GET /Matches/{id}/State, event "match_state") ─
//
// The server owns the match rules. Render this; send `version` back as `expectedVersion` with every action
// (POST /Matches/{id}/PickBan | Scores | Replay | ConfirmResult | Tiebreaker | Forfeit | Undo).
// Unlike the legacy events, match_state uses camelCase field names (same as the REST API).

export type MatchStatus    = "not_started" | "in_progress" | "complete" | "forfeit";
export type NextActionType = "none" | "ban" | "pick" | "set_lineup" | "submit_scores" | "set_tiebreaker" | "replay_or_confirm";
export type PoolMapStatus  = "available" | "banned" | "picked" | "tiebreaker";
export type GameStatus     = "awaiting_lineup" | "awaiting_scores" | "replaying" | "settled";
export type AttemptKind    = "initial" | "replay" | "tie_replay";

export interface MatchState {
    matchId:             number;
    version:             string;
    status:              MatchStatus;
    bestOf:              number | null;
    winsNeeded:          number | null;
    isTeamMatch:         boolean;
    format:              { poolSize: number; bans: number | null; picks: number | null; tiebreakerSource: "pool" | "secret" | null; supported: boolean; problem: string | null };
    sides:               SideState[];         // [side 1, side 2]
    winnerSide:          1 | 2 | null;
    next:                NextAction;
    replayWindow:        { side: 1 | 2; mapId: number; isDecidingMap: boolean } | null;
    sequence:            { step: number; cycle: number; action: "ban" | "pick"; side: 1 | 2; done: boolean; mapId: number | null; pickBanId: number | null }[];
    maps:                { mapId: number; map: object; status: PoolMapStatus; side: 1 | 2 | null; playOrder: number | null }[];
    games:               GameState[];         // maps actually played, in order
    storedResultDiffers: boolean;             // older matches only
    deviations:          string[];            // older matches only
}

export interface SideState {
    side:            1 | 2;
    userId:          number | null;
    teamId:          number | null;
    name:            string | null;
    points:          number;
    replayUsed:      boolean;
    replayAvailable: boolean;
    isWinner:        boolean;
}

export interface NextAction {
    action:                 NextActionType;
    side:                   1 | 2 | null;
    step:                   number | null;
    mapId:                  number | null;
    attempt:                number | null;
    attemptKind:            AttemptKind | null;
    awaitingScoresFrom:     number[];        // user IDs
    awaitingLineupFromSides: number[];
    message:                string | null;
}

export interface GameState {
    order:                  number;
    mapId:                  number;
    map:                    object | null;
    isTiebreaker:           boolean;
    pickedBySide:           1 | 2 | null;
    status:                 GameStatus;
    winnerSide:             1 | 2 | null;
    attempts: {
        number:       number;
        kind:         AttemptKind;
        calledBySide: 1 | 2 | null;
        complete:     boolean;
        side1Total:   number | null;
        side2Total:   number | null;
        scores: { scoreId: number; userId: number | null; scoresaberId: string; side: 1 | 2; score: number; maxScore: number; misses: number; counted: boolean }[];
    }[];
    awaitingScoresFrom:      number[];
    awaitingLineupFromSides: number[];
}

export interface MatchStateEvent {
    event:   "match_state";
    matchId: number;
    data:    { action: string; state: MatchState };
}

// ─── Parent Union ─────────────────────────────────────────────────────────────

export type MatchEvent =
    | ScorePostedEvent
    | ScoreUpdatedEvent
    | PickAddedEvent
    | BanAddedEvent
    | PickBanRemovedEvent
    | MatchUpdatedEvent
    | ReplayCalledEvent
    | MatchStateEvent;

// ─── Parsing Helper ───────────────────────────────────────────────────────────
//
// Usage:
//   socket.onmessage = (msg) => {
//       const event = parseMatchEvent(msg.data);
//       if (!event) return;
//
//       if (event.event === "score_posted")   { /* event.data is FrontFacingMatchScore */ }
//       if (event.event === "replay_called")  { /* event.data is ReplayCalledData      */ }
//       if (event.event === "match_updated")  { /* event.data is Match                 */ }
//   };

export function parseMatchEvent(raw: string): MatchEvent | undefined {
    try {
        const parsed = JSON.parse(raw);
        if (typeof parsed?.event !== "string" || typeof parsed?.matchId !== "number") {
            return undefined;
        }
        return parsed as MatchEvent;
    } catch {
        return undefined;
    }
}
