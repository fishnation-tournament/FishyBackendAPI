using System.Text.Json;
using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
namespace FishyAPI.Tools;

public class MatchTools
{
    private static void PopulateMatchProfiles(MySqlConnection dbConn, ref Match match)
    {
        if (match.Player1ID.HasValue)
            match.Player1User = new FrontFacingUser(UserTools.GetUserById(dbConn, match.Player1ID.Value));
        if (match.Player2ID.HasValue)
            match.Player2User = new FrontFacingUser(UserTools.GetUserById(dbConn, match.Player2ID.Value));
        if (match.CoordinatorUID.HasValue)
            match.CoordinatorUser = new FrontFacingUser(UserTools.GetUserById(dbConn, match.CoordinatorUID.Value));
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
        PopulateMatchProfiles(dbConn, ref match);
        return match;
    }

    public static List<Match> GetMatches(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM matches", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Match> matches = ReadMatches(dbConn, reader);

        return matches;
    }

    public static List<Match> GetMatchesBySeason(MySqlConnection dbConn, int season, bool sortByRound = false)
    {
        string sql = sortByRound
            ? "SELECT * FROM matches WHERE Season = @season ORDER BY RoundOrder IS NULL, RoundOrder ASC"
            : "SELECT * FROM matches WHERE Season = @season";
        MySqlCommand cmd = new MySqlCommand(sql, dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadMatches(dbConn, reader);
    }

    public static Match GetMatchById(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM matches WHERE MatchID = @MID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        Match match = ReadSingleMatch(dbConn, reader);

        return match;
    }

    public static Match CreateMatch(MySqlConnection dbConn, FrontFacingMatch frontFacingMatch)
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO matches
            (Player1UID, Player2UID, Player1Points, Player2Points, MatchDateTime, MatchComplete, WinnerUID, Season, MapPoolID, Status, BestOf, IsStreamed, CoordinatorUID, RoundName, RoundOrder, BanCount, PickCount, LocalMatchID, IsLosersBracket, WinnerGoesToMatch, LoserGoesToMatch)
        VALUES
            (@Player1UID, @Player2UID, @Player1Points, @Player2Points, @MatchDateTime, @MatchComplete, @WinnerUID, @Season, @MapPoolID, @Status, @BestOf, @IsStreamed, @CoordinatorUID, @RoundName, @RoundOrder, @BanCount, @PickCount, @LocalMatchID, @IsLosersBracket, @WinnerGoesToMatch, @LoserGoesToMatch)""
        """, dbConn);
        cmd.Parameters.AddWithValue("@Player1UID", frontFacingMatch.Player1ID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player2UID", frontFacingMatch.Player2ID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player1Points", frontFacingMatch.Player1Score);
        cmd.Parameters.AddWithValue("@Player2Points", frontFacingMatch.Player2Score);
        cmd.Parameters.AddWithValue("@MatchDateTime", frontFacingMatch.MatchDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@MatchComplete", frontFacingMatch.Complete);
        cmd.Parameters.AddWithValue("@WinnerUID", frontFacingMatch.WinnerID.HasValue && frontFacingMatch.WinnerID != 0 ? (object)frontFacingMatch.WinnerID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Season", frontFacingMatch.Season);
        cmd.Parameters.AddWithValue("@MapPoolID", frontFacingMatch.MapPoolID);
        cmd.Parameters.AddWithValue("@Status", frontFacingMatch.Status ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BestOf", frontFacingMatch.BestOf ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@IsStreamed", frontFacingMatch.IsStreamed);
        cmd.Parameters.AddWithValue("@CoordinatorUID", frontFacingMatch.CoordinatorUID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundName", frontFacingMatch.RoundName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundOrder", frontFacingMatch.RoundOrder ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BanCount", frontFacingMatch.BanCount ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PickCount", frontFacingMatch.PickCount ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@LocalMatchID", frontFacingMatch.LocalMatchID);
        cmd.Parameters.AddWithValue("@IsLosersBracket", frontFacingMatch.IsLosersBracket);
        cmd.Parameters.AddWithValue("@WinnerGoesToMatch", frontFacingMatch.WinnerGoesToMatch.HasValue && frontFacingMatch.WinnerGoesToMatch != 0 ? frontFacingMatch.WinnerGoesToMatch.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@LoserGoesToMatch", frontFacingMatch.LoserGoesToMatch.HasValue && frontFacingMatch.LoserGoesToMatch != 0 ? frontFacingMatch.LoserGoesToMatch.Value : DBNull.Value);
        cmd.ExecuteNonQuery();
        return GetMatchById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static void UpdateMatch(MySqlConnection dbConn, Match match)
    {
        Console.WriteLine(JsonSerializer.Serialize(match));
        
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE matches SET
            Player1UID = @Player1UID,
            Player2UID = @Player2UID,
            Player1Points = @Player1Points,
            Player2Points = @Player2Points,
            MatchDateTime = @MatchDateTime,
            MatchComplete = @MatchComplete,
            WinnerUID = @WinnerUID,
            Season = @Season,
            MapPoolID = @MapPoolID,
            Status = @Status,
            BestOf = @BestOf,
            Player1ReplayUsed = @Player1ReplayUsed,
            Player2ReplayUsed = @Player2ReplayUsed,
            IsStreamed = @IsStreamed,
            CoordinatorUID = @CoordinatorUID,
            RoundName = @RoundName,
            RoundOrder = @RoundOrder,
            BanCount = @BanCount,
            PickCount = @PickCount,
            LocalMatchID = @LocalMatchID,
            IsLosersBracket = @IsLosersBracket,
            WinnerGoesToMatch = @WinnerGoesToMatch,
            LoserGoesToMatch = @LoserGoesToMatch
        WHERE MatchID = @MatchID
        """, dbConn);
        cmd.Parameters.AddWithValue("@Player1UID", match.Player1ID.HasValue && match.Player1ID != 0 ? (object)match.Player1ID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Player2UID", match.Player2ID.HasValue && match.Player2ID != 0 ? (object)match.Player2ID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Player1Points", match.Player1Score ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player2Points", match.Player2Score ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchDateTime", match.MatchDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchComplete", match.Complete);
        cmd.Parameters.AddWithValue("@WinnerUID", match.WinnerID.HasValue && match.WinnerID != 0 ? match.WinnerID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@Season", match.Season);
        cmd.Parameters.AddWithValue("@MapPoolID", match.MapPoolID);
        cmd.Parameters.AddWithValue("@Status", match.Status ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BestOf", match.BestOf ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Player1ReplayUsed", match.Player1ReplayUsed);
        cmd.Parameters.AddWithValue("@Player2ReplayUsed", match.Player2ReplayUsed);
        cmd.Parameters.AddWithValue("@IsStreamed", match.IsStreamed);
        cmd.Parameters.AddWithValue("@CoordinatorUID", match.CoordinatorUID.HasValue && match.CoordinatorUID != 0 ? match.CoordinatorUID.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundName", match.RoundName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RoundOrder", match.RoundOrder ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@BanCount", match.BanCount ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PickCount", match.PickCount ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@MatchID", match.MatchID);
        cmd.Parameters.AddWithValue("@LocalMatchID", match.LocalMatchID);
        cmd.Parameters.AddWithValue("@IsLosersBracket", match.IsLosersBracket);
        cmd.Parameters.AddWithValue("@WinnerGoesToMatch", match.WinnerGoesToMatch.HasValue && match.WinnerGoesToMatch != 0 ? match.WinnerGoesToMatch.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@LoserGoesToMatch", match.LoserGoesToMatch.HasValue && match.LoserGoesToMatch != 0 ? match.LoserGoesToMatch.Value : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Marks the replay as used for the specified player and returns the updated match.
    /// Returns null if the match is not found, the player is not in the match, or they already used their replay.
    /// </summary>
    public static Match? SetPlayerReplayUsed(MySqlConnection dbConn, ulong matchId, ulong callingPlayerUID)
    {
        Match match = GetMatchById(dbConn, matchId);
        if (match.MatchID == 0) return null;

        User callingUser = UserTools.GetUserBySSID(dbConn, callingPlayerUID);
        if (callingUser.UID == 0) return null;

        bool isPlayer1 = match.Player1ID == callingUser.UID;
        bool isPlayer2 = match.Player2ID == callingUser.UID;
        if (!isPlayer1 && !isPlayer2) return null;
        if (isPlayer1 && match.Player1ReplayUsed) return null;
        if (isPlayer2 && match.Player2ReplayUsed) return null;

        MySqlCommand cmd = isPlayer1
            ? new MySqlCommand("UPDATE matches SET Player1ReplayUsed = 1 WHERE MatchID = @MatchID", dbConn)
            : new MySqlCommand("UPDATE matches SET Player2ReplayUsed = 1 WHERE MatchID = @MatchID", dbConn);
        cmd.Parameters.AddWithValue("@MatchID", matchId);
        cmd.ExecuteNonQuery();

        return GetMatchById(dbConn, matchId);
    }

    public static List<BulkMatchCreationResult> BulkCreateMatches(MySqlConnection dbConn, List<BulkCreateMatchEntry> entries)
    {
        List<BulkMatchCreationResult> results = new List<BulkMatchCreationResult>();

        using MySqlTransaction transaction = dbConn.BeginTransaction();
        try
        {
            foreach (BulkCreateMatchEntry entry in entries)
            {
                // Insert match
                MySqlCommand matchCmd = new MySqlCommand("""
                INSERT INTO matches
                    (Player1UID, Player2UID, Player1Points, Player2Points, MatchDateTime, MatchComplete, WinnerUID, Season, MapPoolID, Status, BestOf, IsStreamed, CoordinatorUID, RoundName, RoundOrder, BanCount, PickCount, LocalMatchID, IsLosersBracket, WinnerGoesToMatch, LoserGoesToMatch)
                VALUES
                    (@Player1UID, @Player2UID, @Player1Points, @Player2Points, @MatchDateTime, @MatchComplete, @WinnerUID, @Season, @MapPoolID, @Status, @BestOf, @IsStreamed, @CoordinatorUID, @RoundName, @RoundOrder, @BanCount, @PickCount, @LocalMatchID, @IsLosersBracket, @WinnerGoesToMatch, @LoserGoesToMatch)
                """, dbConn, transaction);
                matchCmd.Parameters.AddWithValue("@Player1UID", entry.Player1ID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Player2UID", entry.Player2ID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Player1Points", entry.Player1Score);
                matchCmd.Parameters.AddWithValue("@Player2Points", entry.Player2Score);
                matchCmd.Parameters.AddWithValue("@MatchDateTime", entry.MatchDate.HasValue ? entry.MatchDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@MatchComplete", entry.Complete);
                matchCmd.Parameters.AddWithValue("@WinnerUID", entry.WinnerID.HasValue && entry.WinnerID != 0 ? (object)entry.WinnerID.Value : DBNull.Value);
                matchCmd.Parameters.AddWithValue("@Season", entry.Season);
                matchCmd.Parameters.AddWithValue("@MapPoolID", entry.MapPoolID);
                matchCmd.Parameters.AddWithValue("@Status", entry.Status ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@BestOf", entry.BestOf ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@IsStreamed", entry.IsStreamed);
                matchCmd.Parameters.AddWithValue("@CoordinatorUID", entry.CoordinatorUID ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@RoundName", entry.RoundName ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@RoundOrder", entry.RoundOrder ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@BanCount", entry.BanCount ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@PickCount", entry.PickCount ?? (object)DBNull.Value);
                matchCmd.Parameters.AddWithValue("@LocalMatchID", entry.LocalMatchID);
                matchCmd.Parameters.AddWithValue("@IsLosersBracket", entry.IsLosersBracket);
                matchCmd.Parameters.AddWithValue("@WinnerGoesToMatch", entry.WinnerGoesToMatch.HasValue && entry.WinnerGoesToMatch != 0 ? (object)entry.WinnerGoesToMatch.Value : DBNull.Value);
                matchCmd.Parameters.AddWithValue("@LoserGoesToMatch", entry.LoserGoesToMatch.HasValue && entry.LoserGoesToMatch != 0 ? (object)entry.LoserGoesToMatch.Value : DBNull.Value);
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
    /// The advancing player fills whichever player slot (Player1 or Player2) is currently NULL in the target match.
    /// Does nothing if WinnerGoesToMatch / LoserGoesToMatch is null, or if both slots in the target are already filled.
    /// </summary>
    public static void AdvancePlayersFromMatch(MySqlConnection dbConn, Match finishedMatch)
    {
        if (!finishedMatch.Complete || !finishedMatch.WinnerID.HasValue) return;

        ulong winnerUID = finishedMatch.WinnerID.Value;
        ulong? loserUID = finishedMatch.Player1ID == winnerUID ? finishedMatch.Player2ID : finishedMatch.Player1ID;

        if (finishedMatch.WinnerGoesToMatch.HasValue)
            AdvancePlayerToMatch(dbConn, finishedMatch.WinnerGoesToMatch.Value, winnerUID);

        if (finishedMatch.LoserGoesToMatch.HasValue && loserUID.HasValue)
            AdvancePlayerToMatch(dbConn, finishedMatch.LoserGoesToMatch.Value, loserUID.Value);
    }

    private static void AdvancePlayerToMatch(MySqlConnection dbConn, ulong targetMatchID, ulong playerUID)
    {
        MySqlCommand checkCmd = new MySqlCommand(
            "SELECT Player1UID, Player2UID FROM matches WHERE MatchID = @MatchID LIMIT 1", dbConn);
        checkCmd.Parameters.AddWithValue("@MatchID", targetMatchID);

        using MySqlDataReader reader = checkCmd.ExecuteReader();
        if (!reader.Read()) return;

        bool p1Null = reader.IsDBNull(0);
        bool p2Null = reader.IsDBNull(1);
        ulong? p1UID = p1Null ? null : reader.GetUInt64(0);
        ulong? p2UID = p2Null ? null : reader.GetUInt64(1);
        reader.Close();

        if (p1UID == playerUID || p2UID == playerUID) return;

        string? column = p1Null ? "Player1UID" : (p2Null ? "Player2UID" : null);
        if (column == null) return;

        MySqlCommand updateCmd = new MySqlCommand(
            $"UPDATE matches SET {column} = @PlayerUID WHERE MatchID = @MatchID", dbConn);
        updateCmd.Parameters.AddWithValue("@PlayerUID", playerUID);
        updateCmd.Parameters.AddWithValue("@MatchID", targetMatchID);
        updateCmd.ExecuteNonQuery();
    }
}
