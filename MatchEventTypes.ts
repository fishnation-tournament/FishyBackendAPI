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

// ─── Parent Union ─────────────────────────────────────────────────────────────

export type MatchEvent =
    | ScorePostedEvent
    | ScoreUpdatedEvent
    | PickAddedEvent
    | BanAddedEvent
    | PickBanRemovedEvent
    | MatchUpdatedEvent
    | ReplayCalledEvent;

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
