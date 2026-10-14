using System.Text.Json;
using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.MatchFlow;
namespace FishyAPI.Tools;

public class MatchTools
{
    /// <summary>Explicit column list; Match(reader) reads these by ordinal.</summary>
    public const string MatchColumns =
        "MatchID, Player1UID, Player2UID, Player1Points, Player2Points, MatchDateTime, MatchComplete, WinnerUID, " +
        "Season, MapPoolID, Status, BestOf, Player1ReplayUsed, Player2ReplayUsed, IsStreamed, CoordinatorUID, " +
        "RoundName, RoundOrder, BanCount, PickCount, LocalMatchID, IsLosersBracket, WinnerGoesToMatch, LoserGoesToMatch, " +
        "Team1ID, Team2ID, WinnerTeamID";

    private static void PopulateMatchProfiles(MySqlConnection dbConn, ref Match match)
    {
        if (match.Player1ID.HasValue)
            match.Player1User = new FrontFacingUser(UserTools.GetUserById(dbConn, match.Player1ID.Value));
        if (match.Player2ID.HasValue)
            match.Player2User = new FrontFacingUser(UserTools.GetUserById(dbConn, match.Player2ID.Value));
        if (match.CoordinatorUID.HasValue)
            match.CoordinatorUser = new FrontFacingUser(UserTools.GetUserById(dbConn, match.CoordinatorUID.Value));
        if (match.Team1ID.HasValue)
            match.Team1 = TeamTools.GetTeamById(dbConn, match.Team1ID.Value);
        if (match.Team2ID.HasValue)
            match.Team2 = TeamTools.GetTeamById(dbConn, match.Team2ID.Value);
    }

    public static List<Match> ReadMatches(MySqlConnection dbConn, MySqlDataReader reader)
    {
        List<Match> matches = new List<Match>();
        while (reader.Read())
        {
            matches.Add(new Match(reader));
        }
        reader.Close();

        for (int i = 0; i < matches.Count; i++)
        {
            var tempMatch = matches[i];
            tempMatch.Scores = ScoreTools.GetMatchScores(dbConn, tempMatch.MatchID);
            tempMatch.PickBans = PickBanTools.GetPickBansByMatch(dbConn, tempMatch.MatchID);
            tempMatch.Lineups = MatchLineupTools.GetLineupsByMatch(dbConn, tempMatch.MatchID);
            PopulateMatchProfiles(dbConn, ref tempMatch);
            matches[i] = tempMatch;
        }
        return matches;
    }

    public static Match ReadSingleMatch(MySqlConnection dbConn, MySqlDataReader reader)
    {
        Match match = new Match();

        if (reader.Read())
        {
            match = new Match(reader);
        }
        reader.Close();

        match.Scores = ScoreTools.GetMatchScores(dbConn, match.MatchID);
        match.PickBans = PickBanTools.GetPickBansByMatch(dbConn, match.MatchID);
        match.Lineups = MatchLineupTools.GetLineupsByMatch(dbConn, match.MatchID);
        PopulateMatchProfiles(dbConn, ref match);
        return match;
    }

    public static List<Match> GetMatches(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {MatchColumns} FROM matches", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Match> matches = ReadMatches(dbConn, reader);

        return matches;
    }

    public static List<Match> GetMatchesBySeason(MySqlConnection dbConn, int season, bool sortByRound = false)
    {
        string sql = sortByRound
            ? $"SELECT {MatchColumns} FROM matches WHERE Season = @season ORDER BY RoundOrder IS NULL, RoundOrder ASC"
            : $"SELECT {MatchColumns} FROM matches WHERE Season = @season";
        MySqlCommand cmd = new MySqlCommand(sql, dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadMatches(dbConn, reader);
    }

    public static Match GetMatchById(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT {MatchColumns} FROM matches WHERE MatchID = @MID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        Match match = ReadSingleMatch(dbConn, reader);

        return match;
    }

    /// <summary>Ban and pick counts implied by a match's best-of and pool size (null if the format doesn't work out).</summary>
    public static (object bans, object picks) FormatCounts(MySqlConnection dbConn, ulong mapPoolId, int? bestOf)
    {
        if (bestOf is not int bo) return (DBNull.Value, DBNull.Value);
        (SequencePlan? plan, _) = MatchSequence.Build(bo, MapTools.GetMapsInMapPool(dbConn, mapPoolId).Count);
        return plan == null ? (DBNull.Value, DBNull.Value) : (plan.Bans, plan.Picks);
    }

    /// <summary>Creates a match. Result fields in the request are ignored: results only come from the match's records.</summary>
    public static Match CreateMatch(MySqlConnection dbConn, FrontFacingMatch frontFacingMatch)
    {
        (object bans, object picks) = FormatCounts(dbConn, frontFacingMatch.MapPoolID, frontFacingMatch.BestOf);
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO matches
            (Player1UID, Player2UID, Player1Points, Player2Points, MatchDateTime, MatchComplete, WinnerUID, Season, MapPoolID, Status, BestOf, IsStreamed, CoordinatorUID, RoundName, RoundOrder, BanCount, PickCount, LocalMatchID, IsLosersBracket, WinnerGoesToMatch, LoserGoesToMatch, Team1ID, Team2ID, WinnerTeamID)
        VALUES
            (@Player1UID, @Player2UID, @Player1Points, @Player2Points, @MatchDateTime, @MatchComplete, @WinnerUID, @Season, @MapPoolID, @Status, @BestOf, @IsStreamed, @CoordinatorUID, @RoundName, @RoundOrder, @BanCount, @PickCount, @LocalMatchID, @IsLosersBracket, @WinnerGoesToMatch, @LoserGoesToMatch, @Team1ID, @Team2ID, @WinnerTeamID)
        """, dbConn);
        cmd.Parameters.AddWithValue("@Player1UID", frontFacingMatch.Player1ID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player2UID", frontFacingMatch.Player2ID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player1Points", 0);
        cmd.Parameters.AddWithValue("@Player2Points", 0);
        cmd.Parameters.AddWithValue("@MatchDateTime", frontFacingMatch.MatchDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@MatchComplete", false);
        cmd.Parameters.AddWithValue("@WinnerUID", DBNull.Value);
        cmd.Parameters.AddWithValue("@Season", frontFacingMatch.Season);
        cmd.Parameters.AddWithValue("@MapPoolID", frontFacingMatch.MapPoolID);
        cmd.Parameters.AddWithValue("@Status", "Not Started");
        cmd.Parameters.AddWithValue("@BestOf", frontFacingMatch.BestOf ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsStreamed", frontFacingMatch.IsStreamed);
        cmd.Parameters.AddWithValue("@CoordinatorUID", frontFacingMatch.CoordinatorUID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundName", frontFacingMatch.RoundName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundOrder", frontFacingMatch.RoundOrder ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BanCount", bans);
        cmd.Parameters.AddWithValue("@PickCount", picks);
        cmd.Parameters.AddWithValue("@LocalMatchID", frontFacingMatch.LocalMatchID);
        cmd.Parameters.AddWithValue("@IsLosersBracket", frontFacingMatch.IsLosersBracket);
        cmd.Parameters.AddWithValue("@WinnerGoesToMatch", frontFacingMatch.WinnerGoesToMatch.HasValue && frontFacingMatch.WinnerGoesToMatch != 0 ? frontFacingMatch.WinnerGoesToMatch.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@LoserGoesToMatch", frontFacingMatch.LoserGoesToMatch.HasValue && frontFacingMatch.LoserGoesToMatch != 0 ? frontFacingMatch.LoserGoesToMatch.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Team1ID", frontFacingMatch.Team1ID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Team2ID", frontFacingMatch.Team2ID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@WinnerTeamID", DBNull.Value);
        cmd.ExecuteNonQuery();
        return GetMatchById(dbConn, (ulong)cmd.LastInsertedId);
    }

    /// <summary>
    /// Updates a match's details. Results (points, winner, completion, replays, status, ban/pick counts) are not
    /// written here; MatchActions derives them from the match's records.
    /// </summary>
    public static void UpdateMatchMetadata(MySqlConnection dbConn, Match match)
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE matches SET
            Player1UID = @Player1UID,
            Player2UID = @Player2UID,
            MatchDateTime = @MatchDateTime,
            Season = @Season,
            MapPoolID = @MapPoolID,
            BestOf = @BestOf,
            IsStreamed = @IsStreamed,
            CoordinatorUID = @CoordinatorUID,
            RoundName = @RoundName,
            RoundOrder = @RoundOrder,
            LocalMatchID = @LocalMatchID,
            IsLosersBracket = @IsLosersBracket,
            WinnerGoesToMatch = @WinnerGoesToMatch,
            LoserGoesToMatch = @LoserGoesToMatch,
            Team1ID = @Team1ID,
            Team2ID = @Team2ID
        WHERE MatchID = @MatchID
        """, dbConn);
        cmd.Parameters.AddWithValue("@Player1UID", match.Player1ID.HasValue && match.Player1ID != 0 ? (object)match.Player1ID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Player2UID", match.Player2ID.HasValue && match.Player2ID != 0 ? (object)match.Player2ID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchDateTime", match.MatchDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Season", match.Season);
        cmd.Parameters.AddWithValue("@MapPoolID", match.MapPoolID);
        cmd.Parameters.AddWithValue("@BestOf", match.BestOf ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsStreamed", match.IsStreamed);
        cmd.Parameters.AddWithValue("@CoordinatorUID", match.CoordinatorUID.HasValue && match.CoordinatorUID != 0 ? match.CoordinatorUID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundName", match.RoundName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundOrder", match.RoundOrder ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
        cmd.Parameters.AddWithValue("@LocalMatchID", match.LocalMatchID);
        cmd.Parameters.AddWithValue("@IsLosersBracket", match.IsLosersBracket);
        cmd.Parameters.AddWithValue("@WinnerGoesToMatch", match.WinnerGoesToMatch.HasValue && match.WinnerGoesToMatch != 0 ? match.WinnerGoesToMatch.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@LoserGoesToMatch", match.LoserGoesToMatch.HasValue && match.LoserGoesToMatch != 0 ? match.LoserGoesToMatch.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Team1ID", match.Team1ID.HasValue && match.Team1ID != 0 ? match.Team1ID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Team2ID", match.Team2ID.HasValue && match.Team2ID != 0 ? match.Team2ID.Value : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static List<BulkMatchCreationResult> BulkCreateMatches(MySqlConnection dbConn, List<BulkCreateMatchEntry> entries)
    {
        List<BulkMatchCreationResult> results = new List<BulkMatchCreationResult>();
        List<(object bans, object picks)> formatCounts = entries.Select(e => FormatCounts(dbConn, e.MapPoolID, e.BestOf)).ToList();

        using MySqlTransaction transaction = dbConn.BeginTransaction();
        try
        {
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                BulkCreateMatchEntry entry = entries[entryIndex];
                // Insert match
                MySqlCommand matchCmd = new MySqlCommand("""
                INSERT INTO matches
                    (Player1UID, Player2UID, Player1Points, Player2Points, MatchDateTime, MatchComplete, WinnerUID, Season, MapPoolID, Status, BestOf, IsStreamed, CoordinatorUID, RoundName, RoundOrder, BanCount, PickCount, LocalMatchID, IsLosersBracket, WinnerGoesToMatch, LoserGoesToMatch, Team1ID, Team2ID, WinnerTeamID)
                VALUES
                    (@Player1UID, @Player2UID, @Player1Points, @Player2Points, @MatchDateTime, @MatchComplete, @WinnerUID, @Season, @MapPoolID, @Status, @BestOf, @IsStreamed, @CoordinatorUID, @RoundName, @RoundOrder, @BanCount, @PickCount, @LocalMatchID, @IsLosersBracket, @WinnerGoesToMatch, @LoserGoesToMatch, @Team1ID, @Team2ID, @WinnerTeamID)
                """, dbConn, transaction);
                matchCmd.Parameters.AddWithValue("@Player1UID", entry.Player1ID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Player2UID", entry.Player2ID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Player1Points", 0);
                matchCmd.Parameters.AddWithValue("@Player2Points", 0);
                matchCmd.Parameters.AddWithValue("@MatchDateTime", entry.MatchDate.HasValue ? entry.MatchDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@MatchComplete", false);
                matchCmd.Parameters.AddWithValue("@WinnerUID", DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Season", entry.Season);
                matchCmd.Parameters.AddWithValue("@MapPoolID", entry.MapPoolID);
                matchCmd.Parameters.AddWithValue("@Status", "Not Started");
                matchCmd.Parameters.AddWithValue("@BestOf", entry.BestOf ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@IsStreamed", entry.IsStreamed);
                matchCmd.Parameters.AddWithValue("@CoordinatorUID", entry.CoordinatorUID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@RoundName", entry.RoundName ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@RoundOrder", entry.RoundOrder ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@BanCount", formatCounts[entryIndex].bans);
                matchCmd.Parameters.AddWithValue("@PickCount", formatCounts[entryIndex].picks);
                matchCmd.Parameters.AddWithValue("@LocalMatchID", entry.LocalMatchID);
                matchCmd.Parameters.AddWithValue("@IsLosersBracket", entry.IsLosersBracket);
                matchCmd.Parameters.AddWithValue("@WinnerGoesToMatch", entry.WinnerGoesToMatch.HasValue && entry.WinnerGoesToMatch != 0 ? (object)entry.WinnerGoesToMatch.Value : DBNull.Value);
                matchCmd.Parameters.AddWithValue("@LoserGoesToMatch", entry.LoserGoesToMatch.HasValue && entry.LoserGoesToMatch != 0 ? (object)entry.LoserGoesToMatch.Value : DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Team1ID", entry.Team1ID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Team2ID", entry.Team2ID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@WinnerTeamID", DBNull.Value);
                matchCmd.ExecuteNonQuery();

                ulong matchId = (ulong)matchCmd.LastInsertedId;

                // Insert schedule if provided
                MatchSchedule? schedule = null;
                if (entry.ScheduledAt.HasValue)
                {
                    MySqlCommand schedCmd = new MySqlCommand("""
                    INSERT INTO match_schedule (MatchID, ScheduledAt, Notes)
                    VALUES (@MatchID, @ScheduledAt, @Notes)
                    """, dbConn, transaction);
                    schedCmd.Parameters.AddWithValue("@MatchID", matchId);
                    schedCmd.Parameters.AddWithValue("@ScheduledAt", entry.ScheduledAt.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    schedCmd.Parameters.AddWithValue("@Notes", entry.ScheduleNotes ?? (object)DBNull.Value);
                    schedCmd.ExecuteNonQuery();

                    schedule = new MatchSchedule
                    {
                        ScheduleID = (int)schedCmd.LastInsertedId,
                        MatchID = matchId,
                        ScheduledAt = entry.ScheduledAt.Value,
                        Notes = entry.ScheduleNotes
                    };
                }

                results.Add(new BulkMatchCreationResult
                {
                    Match = new Match { MatchID = matchId },
                    Schedule = schedule
                });
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        // Fetch full match data (with scores and user profiles) after the transaction is closed.
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            result.Match = GetMatchById(dbConn, result.Match.MatchID);
            results[i] = result;
        }

        return results;
    }

    public static void DeleteMatch(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM matches WHERE MatchID = @MID", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// When a match is finished, advances the winner and loser to their next matches.
    /// The advancing player (or team, for team matches) fills whichever slot (1 or 2) is currently NULL in the target match.
    /// Does nothing if WinnerGoesToMatch / LoserGoesToMatch is null, or if both slots in the target are already filled.
    /// </summary>
    public static void AdvancePlayersFromMatch(MySqlConnection dbConn, Match finishedMatch)
    {
        if (!finishedMatch.Complete) return;

        if (finishedMatch.WinnerID.HasValue)
        {
            ulong winnerUID = finishedMatch.WinnerID.Value;
            ulong? loserUID = finishedMatch.Player1ID == winnerUID ? finishedMatch.Player2ID : finishedMatch.Player1ID;

            if (finishedMatch.WinnerGoesToMatch.HasValue)
                AdvanceToMatch(dbConn, finishedMatch.WinnerGoesToMatch.Value, "Player1UID", "Player2UID", winnerUID);

            if (finishedMatch.LoserGoesToMatch.HasValue && loserUID.HasValue)
                AdvanceToMatch(dbConn, finishedMatch.LoserGoesToMatch.Value, "Player1UID", "Player2UID", loserUID.Value);
        }

        if (finishedMatch.WinnerTeamID.HasValue)
        {
            int winnerTeamID = finishedMatch.WinnerTeamID.Value;
            int? loserTeamID = finishedMatch.Team1ID == winnerTeamID ? finishedMatch.Team2ID : finishedMatch.Team1ID;

            if (finishedMatch.WinnerGoesToMatch.HasValue)
                AdvanceToMatch(dbConn, finishedMatch.WinnerGoesToMatch.Value, "Team1ID", "Team2ID", (ulong)winnerTeamID);

            if (finishedMatch.LoserGoesToMatch.HasValue && loserTeamID.HasValue)
                AdvanceToMatch(dbConn, finishedMatch.LoserGoesToMatch.Value, "Team1ID", "Team2ID", (ulong)loserTeamID.Value);
        }
    }

    // slot1Column/slot2Column are always one of the hard-coded pairs above, never user input.
    private static void AdvanceToMatch(MySqlConnection dbConn, ulong targetMatchID, string slot1Column, string slot2Column, ulong participantID)
    {
        MySqlCommand checkCmd = new MySqlCommand(
            $"SELECT {slot1Column}, {slot2Column} FROM matches WHERE MatchID = @MatchID LIMIT 1", dbConn);
        checkCmd.Parameters.AddWithValue("@MatchID", targetMatchID);

        using MySqlDataReader reader = checkCmd.ExecuteReader();
        if (!reader.Read()) return;

        bool s1Null = reader.IsDBNull(0);
        bool s2Null = reader.IsDBNull(1);
        ulong? s1 = s1Null ? null : Convert.ToUInt64(reader.GetValue(0));
        ulong? s2 = s2Null ? null : Convert.ToUInt64(reader.GetValue(1));
        reader.Close();

        if (s1 == participantID || s2 == participantID) return;

        string? column = s1Null ? slot1Column : (s2Null ? slot2Column : null);
        if (column == null) return;

        MySqlCommand updateCmd = new MySqlCommand(
            $"UPDATE matches SET {column} = @ParticipantID WHERE MatchID = @MatchID", dbConn);
        updateCmd.Parameters.AddWithValue("@ParticipantID", participantID);
        updateCmd.Parameters.AddWithValue("@MatchID", targetMatchID);
        updateCmd.ExecuteNonQuery();
    }
}
