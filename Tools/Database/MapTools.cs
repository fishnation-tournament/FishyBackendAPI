using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
namespace FishyAPI.Tools;

public static class MapTools
{
    private static bool MapsTableHasTagsColumn(MySqlConnection dbConn)
    {
        using MySqlCommand cmd = new MySqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'maps'
              AND column_name = 'Tags'
            """,
            dbConn
        );

        object? res = cmd.ExecuteScalar();
        return Convert.ToInt32(res) > 0;
    }

    private static string? SerializeTags(List<string>? tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return null;
        }

        List<string> cleanTags = new List<string>();
        foreach (string tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                cleanTags.Add(tag.Trim());
            }
        }

        return cleanTags.Count == 0 ? null : string.Join(",", cleanTags);
    }

    public static List<Map> ReadMaps(MySqlDataReader reader)
    {
        List<Map> maps = new List<Map>();
        while (reader.Read())
        {
            maps.Add(new Map(reader));
        }

        return maps;
    }

    public static Map ReadSingleMap(MySqlDataReader reader)
    {
        Map map = new Map();
        if (reader.Read())
        {
            map = new Map(reader);
        }

        return map;
    }

    public static List<MapPool> ReadMapPools(MySqlConnection dbConn, MySqlDataReader reader)
    {
        List<MapPool> mapPools = new List<MapPool>();
        while (reader.Read())
        {
            mapPools.Add(new MapPool(reader));
        }
        reader.Close();

        for (int i = 0; i < mapPools.Count; i++)
        {
            var tempMapPool = mapPools[i];
            tempMapPool.Maps = GetMapsInMapPool(dbConn, tempMapPool.MapPoolID);
            mapPools[i] = tempMapPool;
        }

        return mapPools;
    }

    public static MapPool ReadSingleMapPool(MySqlConnection dbConn, MySqlDataReader reader)
    {
        MapPool mapPool = new MapPool();
        if (reader.Read())
        {
            mapPool = new MapPool(reader);
        }
        reader.Close();

        mapPool.Maps = GetMapsInMapPool(dbConn, mapPool.MapPoolID);

        return mapPool;
    }
    
    public static List<MapPool> GetMapPools(MySqlConnection dbConn, bool includePrivate = false)
    {
        string query = includePrivate ? "SELECT * FROM map_pools" : "SELECT * FROM map_pools WHERE Visibility = 'Public'";
        MySqlCommand cmd = new MySqlCommand(query, dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static MapPool GetMapPoolById(MySqlConnection dbConn, ulong MapPoolID, bool includePrivate = false)
    {
        string query = includePrivate ? "SELECT  * FROM map_pools LIMIT 1" : "SELECT * FROM map_pools WHERE Visibility = 'Public' LIMIT 1";
        MySqlCommand cmd = new MySqlCommand(query, dbConn);
        cmd.Parameters.AddWithValue("@MapPoolID", MapPoolID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        MapPool mapPoolRes = ReadSingleMapPool(dbConn, reader);

        return mapPoolRes;
    }

    public static List<MapPool> GetMapPoolByName(MySqlConnection dbConn, string searchTerm, bool includePrivate = false)
    {
        string query = includePrivate ? "SELECT * FROM map_pools WHERE MapPoolName LIKE @searchTerm" : "SELECT * FROM map_pools WHERE MapPoolName LIKE @searchTerm AND Visibility = 'Public'";
        MySqlCommand cmd = new MySqlCommand(query, dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static List<MapPool> GetMapPoolsBySeason(MySqlConnection dbConn, int season, bool includePrivate = false)
    {
        string query = includePrivate ? "SELECT * FROM map_pools WHERE Season = @season" : "SELECT * FROM map_pools WHERE Season = @season AND Visibility = 'Public'";
        MySqlCommand cmd = new MySqlCommand(query, dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static List<Map> GetMaps(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Map> maps = ReadMaps(reader);

        return maps;
    }

    public static List<Map> GetMapsInMapPool(MySqlConnection dbConn, ulong MapPoolID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE MPID = @MapPoolID", dbConn);
        cmd.Parameters.AddWithValue("@MapPoolID", MapPoolID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Map> maps = ReadMaps(reader);

        return maps;
    }

    public static Map GetMapById(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE MapID = @MID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        Map map = ReadSingleMap(reader);

        return map;
    }

    public static List<Map> GetMapsByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE MapName LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Map> maps = ReadMaps(reader);

        return maps;
    }

    public static List<Map> GetMapsBySeason(MySqlConnection dbConn, int season)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE Season = @season", dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<Map> maps = ReadMaps(reader);

        return maps;
    }

    public static Map AddMap(MySqlConnection dbConn, FrontFacingMap frontFacingMap) //POST
    {
        bool hasTagsColumn = MapsTableHasTagsColumn(dbConn);

        string insertSql = hasTagsColumn
            ? """
              INSERT INTO maps
                  (BSMapID, MapName, MapImageLink, MPID, Season, Difficulty, Tags)
              VALUES
                  (@BSMapID, @MapName, @MapImageLink, @MPID, @Season, @Difficulty, @Tags)
              """
            : """
              INSERT INTO maps
                  (BSMapID, MapName, MapImageLink, MPID, Season, Difficulty)
              VALUES
                  (@BSMapID, @MapName, @MapImageLink, @MPID, @Season, @Difficulty)
              """;

        MySqlCommand cmd = new MySqlCommand(insertSql, dbConn);
        cmd.Parameters.AddWithValue("@BSMapID", frontFacingMap.BSMapID);
        cmd.Parameters.AddWithValue("@MapName", frontFacingMap.MapName);
        cmd.Parameters.AddWithValue("@MapImageLink", frontFacingMap.MapImageLink);
        cmd.Parameters.AddWithValue("@Season", frontFacingMap.Season);
        cmd.Parameters.AddWithValue("@Difficulty", frontFacingMap.Difficulty);
        cmd.Parameters.AddWithValue("@MPID", frontFacingMap.MapPoolID);
        if (hasTagsColumn)
        {
            cmd.Parameters.AddWithValue("@Tags", (object?)SerializeTags(frontFacingMap.Tags) ?? DBNull.Value);
        }
        cmd.ExecuteNonQuery();

        return GetMapById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static void UpdateMap(MySqlConnection dbConn, Map map) //PUT
    {
        bool hasTagsColumn = MapsTableHasTagsColumn(dbConn);

        string updateSql = hasTagsColumn
            ? """
              UPDATE maps SET
                  BSMapID = @BSMapID,
                  MapName = @MapName,
                  MapImageLink = @MapImageLink,
                  MPID = @MPID,
                  Season = @Season,
                  Difficulty = @Difficulty,
                  Tags = @Tags
              WHERE MapID = @MapID
              """
            : """
              UPDATE maps SET
                  BSMapID = @BSMapID,
                  MapName = @MapName,
                  MapImageLink = @MapImageLink,
                  MPID = @MPID,
                  Season = @Season,
                  Difficulty = @Difficulty
              WHERE MapID = @MapID
              """;

        MySqlCommand cmd = new MySqlCommand(updateSql, dbConn);
        cmd.Parameters.AddWithValue("@BSMapID", map.BSMapID);
        cmd.Parameters.AddWithValue("@MapName", map.MapName);
        cmd.Parameters.AddWithValue("@MapImageLink", map.MapImageLink);
        cmd.Parameters.AddWithValue("@MPID", map.MapPoolID);
        cmd.Parameters.AddWithValue("@Season", map.Season);
        cmd.Parameters.AddWithValue("@Difficulty", map.Difficulty);
        cmd.Parameters.AddWithValue("@MapID", map.MID);
        if (hasTagsColumn)
        {
            cmd.Parameters.AddWithValue("@Tags", (object?)SerializeTags(map.Tags) ?? DBNull.Value);
        }
        cmd.ExecuteNonQuery();
    }

    public static void DeleteMap(MySqlConnection dbConn, ulong MID) //DELETE
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM maps WHERE MapID = @MID", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        cmd.ExecuteNonQuery();
    }

    public static MapPool AddMapPool(MySqlConnection dbConn, FrontFacingMapPool frontFacingMapPool) //POST
    {
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO map_pools
            (MapPoolName, MapPoolDescription, Season, DownloadLink, OneClickLink, Visibility)
        VALUES
            (@MapPoolName, @MapPoolDescription, @Season,  @DownloadLink, @OneClickLink, @Visibility)
        """, dbConn);
        cmd.Parameters.AddWithValue("@MapPoolName", frontFacingMapPool.MapPoolName);
        cmd.Parameters.AddWithValue("@MapPoolDescription", frontFacingMapPool.MapPoolDescription);
        cmd.Parameters.AddWithValue("@Season", frontFacingMapPool.Season);
        cmd.Parameters.AddWithValue("@DownloadLink", frontFacingMapPool.DownloadLink);
        cmd.Parameters.AddWithValue("@OneClickLink", frontFacingMapPool.OneClickLink);
        cmd.Parameters.AddWithValue("@Visibility", frontFacingMapPool.Visibility);
        cmd.ExecuteNonQuery();

        return GetMapPoolById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static void UpdateMapPool(MySqlConnection dbConn, MapPool mapPool) //PUT
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE map_pools SET
            MapPoolName = @MapPoolName,
            MapPoolDescription = @MapPoolDescription,
            Season = @Season,
            DownloadLink = @DownloadLink,
            OneClickLink = @OneClickLink,
            Visibility = @Visibility
        WHERE MPID = @MPID
        """, dbConn);
        cmd.Parameters.AddWithValue("@MapPoolName", mapPool.MapPoolName);
        cmd.Parameters.AddWithValue("@MapPoolDescription", mapPool.MapPoolDescription);
        cmd.Parameters.AddWithValue("@Season", mapPool.Season);
        cmd.Parameters.AddWithValue("@DownloadLink", mapPool.DownloadLink);
        cmd.Parameters.AddWithValue("@OneClickLink", mapPool.OneClickLink);
        cmd.Parameters.AddWithValue("@Visibility", mapPool.Visibility);
        cmd.Parameters.AddWithValue("@MPID", mapPool.MapPoolID);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteMapPool(MySqlConnection dbConn, ulong MapPoolID) //DELETE
    {
        MySqlCommand cmd = new MySqlCommand("DELETE FROM map_pools WHERE MPID = @MapPoolID", dbConn);
        cmd.Parameters.AddWithValue("@MapPoolID", MapPoolID);
        cmd.ExecuteNonQuery();
    }
}
