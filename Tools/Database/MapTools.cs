using MySql.Data.MySqlClient;
namespace FishyAPI.Tools;

public static class MapTools
{
    public static List<DataTypes.Map> ReadMaps(MySqlDataReader reader)
    {
        List<DataTypes.Map> maps = new List<DataTypes.Map>();
        while (reader.Read())
        {
            DataTypes.Map map = new DataTypes.Map();
            map.MID = reader.GetUInt64(0);
            map.BSMapID = reader.GetString(1);
            map.MapName = reader.GetString(2);
            map.MapImageLink = reader.GetString(3);
            map.MapPoolID = reader.GetUInt64(4);
            map.Season = reader.GetInt32(5);
            map.Difficulty = reader.GetString(6);
            maps.Add(map);
        }

        reader.Close();

        return maps;
    }

    public static DataTypes.Map ReadSingleMap(MySqlDataReader reader)
    {
        DataTypes.Map map = new DataTypes.Map();
        while (reader.Read())
        {
            map.MID = reader.GetUInt64(0);
            map.BSMapID = reader.GetString(1);
            map.MapName = reader.GetString(2);
            map.MapImageLink = reader.GetString(3);
            map.MapPoolID = reader.GetUInt64(4);
            map.Season = reader.GetInt32(5);
            map.Difficulty = reader.GetString(6);
        }
        reader.Close();

        return map;
    }

    public static List<DataTypes.MapPool> ReadMapPools(MySqlConnection dbConn, MySqlDataReader reader)
    {
        List<DataTypes.MapPool> mapPools = new List<DataTypes.MapPool>();
        while (reader.Read())
        {
            DataTypes.MapPool mapPool = new DataTypes.MapPool();
            mapPool.MapPoolID = reader.GetUInt64(0);
            mapPool.MapPoolName = reader.GetString(1);
            mapPool.MapPoolDescription = reader.GetString(2);
            mapPool.Season = reader.GetInt32(3);
            mapPool.Maps = new List<DataTypes.Map>();
            mapPools.Add(mapPool);
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

    public static DataTypes.MapPool ReadSingleMapPool(MySqlConnection dbConn, MySqlDataReader reader)
    {
        DataTypes.MapPool mapPool = new DataTypes.MapPool();
        while (reader.Read())
        {
            mapPool.MapPoolID = reader.GetUInt64(0);
            mapPool.MapPoolName = reader.GetString(1);
            mapPool.MapPoolDescription = reader.GetString(2);
            mapPool.Season = reader.GetInt32(3);
        }
        reader.Close();

        mapPool.Maps = GetMapsInMapPool(dbConn, mapPool.MapPoolID);

        return mapPool;
    }

    public static List<DataTypes.MapPool> GetMapPools(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static DataTypes.MapPool GetMapPoolById(MySqlConnection dbConn, ulong MapPoolID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools WHERE MPID = @MapPoolID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MapPoolID", MapPoolID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.MapPool mapPoolRes = ReadSingleMapPool(dbConn, reader);

        return mapPoolRes;
    }

    public static List<DataTypes.MapPool> GetMapPoolByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools WHERE MapPoolName LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static List<DataTypes.MapPool> GetMapPoolsBySeason(MySqlConnection dbConn, int season)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM map_pools WHERE Season = @season", dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.MapPool> mapPools = ReadMapPools(dbConn, reader);

        return mapPools;
    }

    public static List<DataTypes.Map> GetMaps(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps", dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }

    public static List<DataTypes.Map> GetMapsInMapPool(MySqlConnection dbConn, ulong MapPoolID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE MPID = @MapPoolID", dbConn);
        cmd.Parameters.AddWithValue("@MapPoolID", MapPoolID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }

    public static DataTypes.Map GetMapById(MySqlConnection dbConn, ulong MID)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE MapID = @MID LIMIT 1", dbConn);
        cmd.Parameters.AddWithValue("@MID", MID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        DataTypes.Map map = ReadSingleMap(reader);

        return map;
    }

    public static List<DataTypes.Map> GetMapsByName(MySqlConnection dbConn, string searchTerm)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE MapName LIKE @searchTerm", dbConn);
        cmd.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }

    public static List<DataTypes.Map> GetMapsBySeason(MySqlConnection dbConn, int season)
    {
        MySqlCommand cmd = new MySqlCommand("SELECT * FROM maps WHERE Season = @season", dbConn);
        cmd.Parameters.AddWithValue("@season", season);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }

    public static DataTypes.Map AddMap(MySqlConnection dbConn, FrontFacingDataTypes.FrontFacingMap frontFacingMap) //POST
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

        MySqlCommand getIdQuery = new MySqlCommand("SELECT * FROM maps WHERE MapID = @MapID", dbConn);
        getIdQuery.Parameters.AddWithValue("@MapID", cmd.LastInsertedId);
        using MySqlDataReader reader = getIdQuery.ExecuteReader();
        DataTypes.Map mapOut = new DataTypes.Map();
        if (reader.Read())
        {
            mapOut.MID = reader.GetUInt64(0);
            mapOut.BSMapID = reader.GetString(1);
            mapOut.MapName = reader.GetString(2);
            mapOut.MapImageLink = reader.GetString(3);
            mapOut.MapPoolID = reader.GetUInt64(4);
            mapOut.Season = reader.GetInt32(5);
            mapOut.Difficulty = reader.GetString(6);
        }

        return mapOut;
    }

    public static void UpdateMap(MySqlConnection dbConn, DataTypes.Map map) //PUT
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

    public static DataTypes.MapPool AddMapPool(MySqlConnection dbConn, FrontFacingDataTypes.FrontFacingMapPool frontFacingMapPool) //POST
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

        MySqlCommand getIdQuery = new MySqlCommand("SELECT * FROM map_pools WHERE MPID = @MPID", dbConn);
        getIdQuery.Parameters.AddWithValue("@MPID", cmd.LastInsertedId);
        using MySqlDataReader reader = getIdQuery.ExecuteReader();
        DataTypes.MapPool mapPoolOut = new DataTypes.MapPool();
        if (reader.Read())
        {
            mapPoolOut.MapPoolID = reader.GetUInt64(0);
            mapPoolOut.MapPoolName = reader.GetString(1);
            mapPoolOut.MapPoolDescription = reader.GetString(2);
            mapPoolOut.Season = reader.GetInt32(3);
        }

        return mapPoolOut;
    }

    public static void UpdateMapPool(MySqlConnection dbConn, DataTypes.MapPool mapPool) //PUT
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