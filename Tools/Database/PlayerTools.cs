using System.Text.Json;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools;

public static class PlayerTools
{
    private static List<Player> ReadResults(MySqlConnection conn, MySqlDataReader reader)
    {
        List<Player> players = new List<Player>();
        while (reader.Read())
        {
            players.Add(new Player(reader));
        }
        
        reader.Close();

        for (int i = 0; i < players.Count; i++)
        {
            var tempPlayer = players[i];
            tempPlayer.User = new FrontFacingUser(UserTools.GetUserById(conn, players[i].UID));
            
            players[i] = tempPlayer;
        }

        return players;
    }

    private static Player ReadSingleResult(MySqlConnection conn, MySqlDataReader reader)
    {
        Player player = new Player();
        if (reader.Read())
        {
            player = new Player(reader);
        }
        
        reader.Close();
        player.User = new FrontFacingUser(UserTools.GetUserById(conn, player.UID)); 

        return player;
    }
    
    public static List<Player> GetPlayers(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM players", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Player> players = ReadResults(dbConn, reader);

        return players;
    }

    public static Player GetPlayerByID(MySqlConnection dbConn, int id)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM players WHERE PlayerID = @id LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@id", id);
        using MySqlDataReader reader = cmd.ExecuteReader();
        Player player = ReadSingleResult(dbConn, reader);

        return player;
    }

    public static List<Player> GetPlayersByName(MySqlConnection dbConn, string name)
    {
        MySqlCommand cmd = new MySqlCommand("""
        SELECT p.PlayerID, p.UID, p.Season, p.Seed, p.ScoresaberID
        FROM players p
        INNER JOIN users u ON p.UID = u.UserID
        WHERE u.Username LIKE @searchTerm
        """, dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{name}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Player> players = ReadResults(dbConn, reader);

        return players;
    }
    
    public static List<Player> GetPlayerByUID(MySqlConnection dbConn, List<ulong> ssids)
    {
        if (ssids.Count == 0) return new List<Player>();

        List<string> parameterNames = new List<string>();
        for (int i = 0; i < ssids.Count; i++)
        {
            parameterNames.Add($"@uid{i}");
        }

        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM players WHERE UID IN ({string.Join(", ", parameterNames)})", dbConn);
        for (int i = 0; i < ssids.Count; i++)
        {
            cmd.Parameters.AddWithValue(parameterNames[i], ssids[i]);
        }

        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Player> players = ReadResults(dbConn, reader);

        return players;
    }
    
    public static List<Player> GetPlayersBySeason(MySqlConnection dbConn, int season)
    {
        MySqlCommand cmd = new MySqlCommand("""
                                            SELECT PlayerID, UID, Season, Seed, ScoresaberID
                                            FROM players
                                            WHERE Season = @season
                                            """, dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Player> players = ReadResults(dbConn, reader);

        return players;
    }

    public static bool IsPlayerInSeason(MySqlConnection dbConn, ulong uid, int seasonId)
    {
        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM players WHERE Season = @season AND UID = @uid LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@season", seasonId);
        cmd.Parameters.AddWithValue("@uid", uid);
        using MySqlDataReader reader = cmd.ExecuteReader();
        Player player = ReadSingleResult(dbConn, reader);
        
        reader.Close();
        return player.PlayerID != null;
            
    }

    public static Player AddPlayer(MySqlConnection dbConn, Player player) //POST
    {
        // Auto-populate ScoresaberID from the linked user's SSID if not already set
        if (!player.ScoresaberID.HasValue)
        {
            User user = UserTools.GetUserById(dbConn, player.UID);
            player.ScoresaberID = user.SSID;
        }

        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO players
            (UID, Season, Seed, ScoresaberID)
        VALUES
            (@UID, @Season, @Seed, @ScoresaberID)
        """, dbConn);
        cmd.Parameters.AddWithValue("@UID", player.UID);
        cmd.Parameters.AddWithValue("@Season", player.Season ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Seed", player.Seed ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ScoresaberID", player.ScoresaberID ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        return GetPlayerByID(dbConn, (int)cmd.LastInsertedId);
    }

    public static void UpdatePlayer(MySqlConnection dbConn, Player player) //PUT
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE players SET
            UID = @UID,
            Season = @Season,
            Seed = @Seed,
            ScoresaberID = @ScoresaberID
        WHERE PlayerID = @PlayerID
        """, dbConn);
        cmd.Parameters.AddWithValue("@UID", player.UID);
        cmd.Parameters.AddWithValue("@Season", player.Season ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Seed", player.Seed ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ScoresaberID", player.ScoresaberID ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@PlayerID", player.PlayerID);
        cmd.ExecuteNonQuery();
    }

    public static Player? GetPlayerByScoresaberID(MySqlConnection dbConn, ulong scoresaberID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT * FROM players WHERE ScoresaberID = @ScoresaberID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@ScoresaberID", scoresaberID);
        using MySqlDataReader reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            reader.Close();
            return null;
        }

        Player player = new Player(reader);
        reader.Close();
        player.User = new FrontFacingUser(UserTools.GetUserById(dbConn, player.UID));
        return player;
    }

    public static void DeletePlayer(MySqlConnection dbConn, int playerID) //DELETE
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM players WHERE PlayerID = @PlayerID", dbConn);
        cmd.Parameters.AddWithValue("@PlayerID", playerID);
        cmd.ExecuteNonQuery();
    }
}