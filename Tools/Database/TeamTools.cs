using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;

namespace FishyAPI.Tools;

public static class TeamTools
{
    private const string TeamColumns = "TeamID, SeasonID, TeamName, TeamLogoLink, Seed, CaptainUID, CreatedAt";
    private const string MemberColumns = "TeamID, SeasonID, UserID, JoinedAt";

    private static List<Team> ReadTeams(MySqlConnection conn, MySqlDataReader reader)
    {
        List<Team> teams = new();
        while (reader.Read())
            teams.Add(new Team(reader));
        reader.Close();

        for (int i = 0; i < teams.Count; i++)
        {
            var team = teams[i];
            team.Members = GetTeamMembers(conn, team.TeamID);
            teams[i] = team;
        }
        return teams;
    }

    public static List<Team> GetTeamsBySeason(MySqlConnection conn, int seasonId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {TeamColumns} FROM teams WHERE SeasonID = @SeasonID ORDER BY Seed IS NULL, Seed, TeamName",
            conn);
        cmd.Parameters.AddWithValue("@SeasonID", seasonId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadTeams(conn, reader);
    }

    public static List<Team> GetTeamsByUser(MySqlConnection conn, ulong userId)
    {
        MySqlCommand cmd = new MySqlCommand("""
            SELECT t.TeamID, t.SeasonID, t.TeamName, t.TeamLogoLink, t.Seed, t.CaptainUID, t.CreatedAt
            FROM teams t
            INNER JOIN team_members tm ON tm.TeamID = t.TeamID
            WHERE tm.UserID = @UserID
            ORDER BY t.SeasonID DESC
            """, conn);
        cmd.Parameters.AddWithValue("@UserID", userId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        return ReadTeams(conn, reader);
    }

    public static Team? GetTeamById(MySqlConnection conn, int teamId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {TeamColumns} FROM teams WHERE TeamID = @TeamID LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Team> teams = ReadTeams(conn, reader);
        return teams.Count == 0 ? null : teams[0];
    }

    public static List<TeamMember> GetTeamMembers(MySqlConnection conn, int teamId)
    {
        MySqlCommand cmd = new MySqlCommand(
            $"SELECT {MemberColumns} FROM team_members WHERE TeamID = @TeamID ORDER BY JoinedAt",
            conn);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        List<TeamMember> members = new();
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                members.Add(new TeamMember(reader));
        }

        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            member.User = new FrontFacingUser(UserTools.GetUserById(conn, member.UserID));
            members[i] = member;
        }
        return members;
    }

    public static bool IsTeamMember(MySqlConnection conn, int teamId, ulong userId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT 1 FROM team_members WHERE TeamID = @TeamID AND UserID = @UserID LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        cmd.Parameters.AddWithValue("@UserID", userId);
        return cmd.ExecuteScalar() != null;
    }

    public static int CountTeamMembers(MySqlConnection conn, int teamId)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM team_members WHERE TeamID = @TeamID", conn);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Creates the team and, when a captain is given, adds them as the first member in the same transaction.
    /// </summary>
    public static Team CreateTeam(MySqlConnection conn, CreateTeamRequest request)
    {
        using MySqlTransaction transaction = conn.BeginTransaction();
        int teamId;
        try
        {
            MySqlCommand cmd = new MySqlCommand("""
                INSERT INTO teams (SeasonID, TeamName, TeamLogoLink, Seed, CaptainUID)
                VALUES (@SeasonID, @TeamName, @TeamLogoLink, @Seed, @CaptainUID)
                """, conn, transaction);
            cmd.Parameters.AddWithValue("@SeasonID", request.SeasonID);
            cmd.Parameters.AddWithValue("@TeamName", request.TeamName);
            cmd.Parameters.AddWithValue("@TeamLogoLink", request.TeamLogoLink ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Seed", request.Seed ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CaptainUID", request.CaptainUID ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
            teamId = (int)cmd.LastInsertedId;

            if (request.CaptainUID.HasValue)
                InsertMember(conn, transaction, teamId, request.SeasonID, request.CaptainUID.Value);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return GetTeamById(conn, teamId)!.Value;
    }

    public static Team? UpdateTeam(MySqlConnection conn, int teamId, UpdateTeamRequest request)
    {
        Team? existing = GetTeamById(conn, teamId);
        if (!existing.HasValue) return null;
        Team team = existing.Value;

        MySqlCommand cmd = new MySqlCommand("""
            UPDATE teams SET
                TeamName = @TeamName,
                TeamLogoLink = @TeamLogoLink,
                Seed = @Seed,
                CaptainUID = @CaptainUID
            WHERE TeamID = @TeamID
            """, conn);
        cmd.Parameters.AddWithValue("@TeamName", request.TeamName ?? team.TeamName);
        cmd.Parameters.AddWithValue("@TeamLogoLink",
            request.ClearTeamLogoLink ? DBNull.Value : (object?)(request.TeamLogoLink ?? team.TeamLogoLink) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Seed",
            request.ClearSeed ? DBNull.Value : (object?)(request.Seed ?? team.Seed) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CaptainUID",
            request.ClearCaptain ? DBNull.Value : (object?)(request.CaptainUID ?? team.CaptainUID) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        cmd.ExecuteNonQuery();
        return GetTeamById(conn, teamId);
    }

    public static bool DeleteTeam(MySqlConnection conn, int teamId)
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM teams WHERE TeamID = @TeamID", conn);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static void InsertMember(MySqlConnection conn, MySqlTransaction? transaction, int teamId, int seasonId, ulong userId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "INSERT INTO team_members (TeamID, SeasonID, UserID) VALUES (@TeamID, @SeasonID, @UserID)",
            conn, transaction);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        cmd.Parameters.AddWithValue("@SeasonID", seasonId);
        cmd.Parameters.AddWithValue("@UserID", userId);
        cmd.ExecuteNonQuery();
    }

    public static void AddTeamMember(MySqlConnection conn, Team team, ulong userId)
        => InsertMember(conn, null, team.TeamID, team.SeasonID, userId);

    /// <summary>Removes a member; if they were captain, the team's captain is cleared.</summary>
    public static bool RemoveTeamMember(MySqlConnection conn, int teamId, ulong userId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM team_members WHERE TeamID = @TeamID AND UserID = @UserID",
            conn);
        cmd.Parameters.AddWithValue("@TeamID", teamId);
        cmd.Parameters.AddWithValue("@UserID", userId);
        bool removed = cmd.ExecuteNonQuery() > 0;

        if (removed)
        {
            MySqlCommand captainCmd = new MySqlCommand(
                "UPDATE teams SET CaptainUID = NULL WHERE TeamID = @TeamID AND CaptainUID = @UserID",
                conn);
            captainCmd.Parameters.AddWithValue("@TeamID", teamId);
            captainCmd.Parameters.AddWithValue("@UserID", userId);
            captainCmd.ExecuteNonQuery();
        }

        return removed;
    }
}
