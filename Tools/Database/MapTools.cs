using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
namespace FishyAPI.Tools;

public static class MapTools
{
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

    public static List<MapPool> GetMapPools(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static MapPool GetMapPoolById(MySqlConnection dbConn, ulong MapPoolID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools WHERE MPID = @MapPoolID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MapPoolID", MapPoolID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        MapPool mapPoolRes = ReadSingleMapPool(dbConn, reader);

        return mapPoolRes;
    }

    public static List<MapPool> GetMapPoolByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools WHERE MapPoolName LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static List<MapPool> GetMapPoolsBySeason(MySqlConnection dbConn, int season)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools WHERE Season = @season", dbConn);
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
        MySqlCommand cmd = new MySqlCommand("""
        INSERT INTO maps
            (BSMapID, MapName, MapImageLink, MPID, Season, Difficulty)
        VALUES
            (@BSMapID, @MapName, @MapImageLink, @MPID, @Season, @Difficulty)
        """, dbConn);
        cmd.Parameters.AddWithValue("@BSMapID", frontFacingMap.BSMapID);
        cmd.Parameters.AddWithValue("@MapName", frontFacingMap.MapName);
        cmd.Parameters.AddWithValue("@MapImageLink", frontFacingMap.MapImageLink);
        cmd.Parameters.AddWithValue("@MPID", frontFacingMap.MapPoolID);
        cmd.Parameters.AddWithValue("@Season", frontFacingMap.Season);
        cmd.Parameters.AddWithValue("@Difficulty", frontFacingMap.Difficulty);
        cmd.ExecuteNonQuery();

        return GetMapById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static void UpdateMap(MySqlConnection dbConn, Map map) //PUT
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE maps SET
            BSMapID = @BSMapID,
            MapName = @MapName,
            MapImageLink = @MapImageLink,
            MPID = @MPID,
            Season = @Season,
            Difficulty = @Difficulty
        WHERE MapID = @MapID
        """, dbConn);
        cmd.Parameters.AddWithValue("@BSMapID", map.BSMapID);
        cmd.Parameters.AddWithValue("@MapName", map.MapName);
        cmd.Parameters.AddWithValue("@MapImageLink", map.MapImageLink);
        cmd.Parameters.AddWithValue("@MPID", map.MapPoolID);
        cmd.Parameters.AddWithValue("@Season", map.Season);
        cmd.Parameters.AddWithValue("@Difficulty", map.Difficulty);
        cmd.Parameters.AddWithValue("@MapID", map.MID);
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
            (MapPoolName, MapPoolDescription, Season)
        VALUES
            (@MapPoolName, @MapPoolDescription, @Season)
        """, dbConn);
        cmd.Parameters.AddWithValue("@MapPoolName", frontFacingMapPool.MapPoolName);
        cmd.Parameters.AddWithValue("@MapPoolDescription", frontFacingMapPool.MapPoolDescription);
        cmd.Parameters.AddWithValue("@Season", frontFacingMapPool.Season);
        cmd.ExecuteNonQuery();

        return GetMapPoolById(dbConn, (ulong)cmd.LastInsertedId);
    }

    public static void UpdateMapPool(MySqlConnection dbConn, MapPool mapPool) //PUT
    {
        MySqlCommand cmd = new MySqlCommand("""
        UPDATE map_pools SET
            MapPoolName = @MapPoolName,
            MapPoolDescription = @MapPoolDescription,
            Season = @Season
        WHERE MPID = @MPID
        """, dbConn);
        cmd.Parameters.AddWithValue("@MapPoolName", mapPool.MapPoolName);
        cmd.Parameters.AddWithValue("@MapPoolDescription", mapPool.MapPoolDescription);
        cmd.Parameters.AddWithValue("@Season", mapPool.Season);
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
