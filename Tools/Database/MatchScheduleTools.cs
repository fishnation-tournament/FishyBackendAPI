using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public class MatchScheduleTools
{
    private static List<MatchSchedule> ReadSchedules(MySqlDataReader reader)
    {
        List<MatchSchedule> schedules = new List<MatchSchedule>();
        while (reader.Read())
            schedules.Add(new MatchSchedule(reader));
        return schedules;
    }

    private static List<MatchSchedule> PopulateScheduleMatches(MySqlConnection dbConn, List<MatchSchedule> schedules)
    {
        for (int i = 0; i < schedules.Count; i++)
        {
            var s = schedules[i];
            s.Match = MatchTools.GetMatchById(dbConn, s.MatchID);
            schedules[i] = s;
        }
        return schedules;
    }

    public static List<MatchSchedule> GetSchedulesByMatch(MySqlConnection dbConn, ulong matchId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT ScheduleID, MatchID, ScheduledAt, Notes FROM match_schedule WHERE MatchID = @MatchID ORDER BY ScheduledAt",
            dbConn);
        cmd.Parameters.AddWithValue("@MatchID", matchId);
        List<MatchSchedule> schedules;
        using (MySqlDataReader reader = cmd.ExecuteReader())
            schedules = ReadSchedules(reader);
        return PopulateScheduleMatches(dbConn, schedules);
    }

    public static List<Match> GetUpcomingSchedules(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {MatchTools.MatchColumns} FROM matches WHERE MatchDateTime >= UTC_TIMESTAMP() ORDER BY MatchDateTime",
            dbConn);
        List<Match> schedules;
        using (MySqlDataReader reader = cmd.ExecuteReader())
            schedules = MatchTools.ReadMatches(dbConn, reader);
        return schedules;
    }
    
    public static List<Match> GetSchedules(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {MatchTools.MatchColumns} FROM matches ORDER BY MatchDateTime",
            dbConn);
        List<Match> schedules;
        using (MySqlDataReader reader = cmd.ExecuteReader())
            schedules = MatchTools.ReadMatches(dbConn, reader);
        return schedules;
    }

    public static MatchSchedule? GetScheduleById(MySqlConnection dbConn, int scheduleId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT ScheduleID, MatchID, ScheduledAt, Notes FROM match_schedule WHERE ScheduleID = @ID LIMIT 1",
            dbConn);
        cmd.Parameters.AddWithValue("@ID", scheduleId);
        MatchSchedule? schedule = null;
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
            if (reader.Read())
                schedule = new MatchSchedule(reader);
        }
        if (!schedule.HasValue) return null;
        var s = schedule.Value;
        s.Match = MatchTools.GetMatchById(dbConn, s.MatchID);
        return s;
    }

    public static MatchSchedule CreateSchedule(MySqlConnection dbConn, CreateMatchScheduleRequest request)
    {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO match_schedule (MatchID, ScheduledAt, Notes) VALUES (@MatchID, @ScheduledAt, @Notes)",
            dbConn);
        cmd.Parameters.AddWithValue("@MatchID", request.MatchID);
        cmd.Parameters.AddWithValue("@ScheduledAt", request.ScheduledAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@Notes", request.Notes ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        int newId = (int)cmd.LastInsertedId;
        return GetScheduleById(dbConn, newId)!.Value;
    }

    public static bool DeleteSchedule(MySqlConnection dbConn, int scheduleId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM match_schedule WHERE ScheduleID = @ID",
            dbConn);
        cmd.Parameters.AddWithValue("@ID", scheduleId);
        return cmd.ExecuteNonQuery() > 0;
    }
}
