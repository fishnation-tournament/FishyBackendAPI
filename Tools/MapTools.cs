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
            maps.Add(map);
        }
        return maps;
    }
    
    public static DataTypes.Map ReadSingleMap(MySqlDataReader reader)
    {
        DataTypes.Map map = new DataTypes.Map();
        map.MID = reader.GetUInt64(0);
        map.BSMapID = reader.GetString(1);
        map.MapName = reader.GetString(2);
        map.MapImageLink = reader.GetString(3);
        map.MapPoolID = reader.GetUInt64(4);
        map.Season = reader.GetInt32(5);
        return map;
    }
    
    public static List<DataTypes.MapPool> ReadMapPools(SQLInteraction interactionHelper, MySqlDataReader reader)
    {
        List<DataTypes.MapPool> mapPools = new List<DataTypes.MapPool>();
        while (reader.Read())
        {
            DataTypes.MapPool mapPool = new DataTypes.MapPool();
            mapPool.MapPoolID = reader.GetUInt64(0);
            mapPool.MapPoolName = reader.GetString(1);
            mapPool.MapPoolDescription = reader.GetString(2);
            mapPool.Season = reader.GetInt32(3);
            mapPool.Maps = GetMapsInMapPool(interactionHelper, mapPool.MapPoolID);
            mapPools.Add(mapPool);
        }
        return mapPools;
    }
    
    public static DataTypes.MapPool ReadSingleMapPool(SQLInteraction interactionHelper, MySqlDataReader reader)
    {
        DataTypes.MapPool mapPool = new DataTypes.MapPool();
        mapPool.MapPoolID = reader.GetUInt64(0);
        mapPool.MapPoolName = reader.GetString(1);
        mapPool.MapPoolDescription = reader.GetString(2);
        mapPool.Season = reader.GetInt32(3);
        mapPool.Maps = GetMapsInMapPool(interactionHelper, mapPool.MapPoolID);
        return mapPool;
    }
    
    public static List<DataTypes.MapPool> GetMapPools(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM map_pools";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);
        reader.Close();

        return mapPools;
    }
    
    public static DataTypes.MapPool GetMapPoolById(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "SELECT * FROM map_pools WHERE MapPoolID = " + MapPoolID + " LIMIT 1";
        var reader = interactionHelper.GetReader(query);
        DataTypes.MapPool mapPoolRes = ReadSingleMapPool(interactionHelper, reader);
        reader.Close();

        return mapPoolRes;
    }
    
    public static List<DataTypes.MapPool> SearchMapPoolByName(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM map_pools WHERE MapPoolName LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);
        reader.Close();

        return mapPools;
    }
    
    public static List<DataTypes.MapPool> SearchMapPoolsBySeason(SQLInteraction interactionHelper, int season)
    {
        string query = "SELECT * FROM map_pools WHERE Season = " + season;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);
        reader.Close();

        return mapPools;
    }
    
    public static List<DataTypes.Map> GetMapsInMapPool(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "SELECT * FROM maps WHERE MapPoolID = " + MapPoolID;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> mapRes = ReadMaps(reader);
        reader.Close();

        return mapRes;
    }
    
    public static DataTypes.Map GetMapById(SQLInteraction interactionHelper, ulong MID)
    {
        string query = "SELECT * FROM maps WHERE MID = " + MID + " LIMIT 1";
        var reader = interactionHelper.GetReader(query);
        DataTypes.Map mapRes = ReadSingleMap(reader);
        reader.Close();

        return mapRes;
    }

    public static List<DataTypes.Map> SearchMapByName(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM maps WHERE MapName LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> maps = ReadMaps(reader);
        reader.Close();

        return maps;
    }
    
    public static List<DataTypes.Map> SearchMapsBySeason(SQLInteraction interactionHelper, int season)
    {
        string query = "SELECT * FROM maps WHERE Season = " + season;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> maps = ReadMaps(reader);
        reader.Close();

        return maps;
    }
    
    public static void UpdateMap(SQLInteraction interactionHelper, DataTypes.Map map)
    {
        string query = "UPDATE maps SET BSMapID = '" + map.BSMapID + "', MapName = '" + map.MapName + "', MapImageLink = '" + map.MapImageLink + "', MapPoolID = " + map.MapPoolID + ", Season = " + map.Season + " WHERE MID = " + map.MID;
        interactionHelper.SendCommand(query);
    }
    
    public static void AddMap(SQLInteraction interactionHelper, DataTypes.Map map)
    {
        string query = "INSERT INTO maps (BSMapID, MapName, MapImageLink, MapPoolID, Season) VALUES ('" + map.BSMapID + "', '" + map.MapName + "', '" + map.MapImageLink + "', " + map.MapPoolID + ", " + map.Season + ")";
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteMap(SQLInteraction interactionHelper, ulong MID)
    {
        string query = "DELETE FROM maps WHERE MID = " + MID;
        interactionHelper.SendCommand(query);
    }
    
    public static void AddMapPool(SQLInteraction interactionHelper, DataTypes.MapPool mapPool)
    {
        string query = "INSERT INTO mappools (MapPoolName, MapPoolDescription, Season) VALUES ('" + mapPool.MapPoolName + "', '" + mapPool.MapPoolDescription + "', " + mapPool.Season + ")";
        interactionHelper.SendCommand(query);
    }
    
    public static void UpdateMapPool(SQLInteraction interactionHelper, DataTypes.MapPool mapPool)
    {
        string query = "UPDATE mappools SET MapPoolName = '" + mapPool.MapPoolName + "', MapPoolDescription = '" + mapPool.MapPoolDescription + "', Season = " + mapPool.Season + " WHERE MapPoolID = " + mapPool.MapPoolID;
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteMapPool(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "DELETE FROM mappools WHERE MapPoolID = " + MapPoolID;
        interactionHelper.SendCommand(query);
    }
}