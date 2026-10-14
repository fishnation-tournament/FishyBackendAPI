// ─── Supporting Types ────────────────────────────────────────────────────────
//
// Every event uses the same camelCase field names as the REST API.

export interface FrontFacingUser {
    uid:               number;
    ssid:              string | null;   // serialised as string (ulong)
    optBLUID:          string | null;   // serialised as string (ulong)
    username:          string;
    userPfpLink:       string | null;
    userBio:           string | null;
    discordID:         string | null;   // serialised as string (ulong)
    discordUsername:   string | null;
    registrationDate:  string;          // ISO 8601
    roles:             string[];        // role keys, e.g. ["user", "organizer"]
    title:             string | null;   // display text, not used for access
    scoreSaberProfile: object | null;   // raw JSON blob
    beatLeaderProfile: object | null;   // raw JSON blob
}

export interface MatchScore {
    scoreID:           number;
    uid:               string;          // serialised as string (ulong)
    mapID:             number;
    matchID:           number;
    score:             number;
    maxScore:          number;
    misses:            number;
    /** A replay was called after this score. The score is still a required benchmark for the replaying player. */
    replayCalledAfter: boolean;
    isReplayAttempt:   boolean;
    teamID:            number | null;
}

export interface PickBan {
    pickBanID: number;
    matchID:   number;
    isPick:    boolean;   // true = pick, false = ban
    playerID:  number;
    teamID:    number | null;
    mapID:     number | null;
    map:       object | null;
}

export interface MatchLineup {
    lineupID: number;
    matchID:  number;
    mapID:    number;
    teamID:   number;
    userID:   number;
}

export interface Match {
    matchID:           number;
    player1ID:         number | null;
    player2ID:         number | null;
    complete:          boolean;
    winnerID:          number | null;
    player1Score:      number | null;
    player2Score:      number | null;
    matchDate:         string | null;   // ISO 8601
    scores:            MatchScore[];
    pickBans:          PickBan[];
    lineups:           MatchLineup[];
    season:            number;
    mapPoolID:         number;
    status:            string | null;
    bestOf:            number | null;
    player1ReplayUsed: boolean;
    player2ReplayUsed: boolean;
    isStreamed:        boolean;
    coordinatorUID:    number | null;
    roundName:         string | null;
    roundOrder:        number | null;
    banCount:          number | null;
    pickCount:         number | null;
    localMatchID:      number;
    isLosersBracket:   boolean;
    winnerGoesToMatch: number | null;
    loserGoesToMatch:  number | null;
    team1ID:           number | null;
    team2ID:           number | null;
    winnerTeamID:      number | null;
    player1User:       FrontFacingUser | null;
    player2User:       FrontFacingUser | null;
    coordinatorUser:   FrontFacingUser | null;
    team1:             object | null;
    team2:             object | null;
}

// ─── Individual Event Types ───────────────────────────────────────────────────

/** Sent after every match action, alongside match_state, with the full match record. */
export interface MatchUpdatedEvent {
    event:   "match_updated";
    matchId: number;
    data:    Match;
}

export interface LineupAddedEvent {
    event:   "lineup_added";
    matchId: number;
    data:    MatchLineup;
}

export interface LineupRemovedEvent {
    event:   "lineup_removed";
    matchId: number;
    data:    { lineupId: number };
}

// ─── Server-computed match state (GET /Matches/{id}/State, event "match_state") ─
//
// The server owns the match rules. Render this; send `version` back as `expectedVersion` with every action
// (POST /Matches/{id}/PickBan | Scores | Replay | ConfirmResult | Tiebreaker | Forfeit | Undo).

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
    | MatchStateEvent
    | MatchUpdatedEvent
    | LineupAddedEvent
    | LineupRemovedEvent;

// ─── Parsing Helper ───────────────────────────────────────────────────────────
//
// Usage:
//   socket.onmessage = (msg) => {
//       const event = parseMatchEvent(msg.data);
//       if (!event) return;
//
//       if (event.event === "match_state")   { /* event.data is { action, state: MatchState } */ }
//       if (event.event === "match_updated") { /* event.data is Match                         */ }
//       if (event.event === "lineup_added")  { /* event.data is MatchLineup                   */ }
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
