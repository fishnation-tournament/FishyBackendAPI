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
        while(reader.Read())
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
            mapPool.Maps = new List<DataTypes.Map>();
            mapPools.Add(mapPool);
        }
        reader.Close();
        
        for(int i = 0; i < mapPools.Count; i++)
        {
            var tempMapPool = mapPools[i];
            tempMapPool.Maps = GetMapsInMapPool(interactionHelper, tempMapPool.MapPoolID);
            mapPools[i] = tempMapPool;
        }

        return mapPools;
    }
    
    public static DataTypes.MapPool ReadSingleMapPool(SQLInteraction interactionHelper, MySqlDataReader reader)
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
        
        mapPool.Maps = GetMapsInMapPool(interactionHelper, mapPool.MapPoolID);
        
        return mapPool;
    }
    
    public static List<DataTypes.MapPool> GetMapPools(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM map_pools";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);

        return mapPools;
    }
    
    public static DataTypes.MapPool GetMapPoolById(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "SELECT * FROM map_pools WHERE MPID = " + MapPoolID + " LIMIT 1";
        var reader = interactionHelper.GetReader(query);
        DataTypes.MapPool mapPoolRes = ReadSingleMapPool(interactionHelper, reader);

        return mapPoolRes;
    }
    
    public static List<DataTypes.MapPool> GetMapPoolByName(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM map_pools WHERE MapPoolName LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);

        return mapPools;
    }
    
    public static List<DataTypes.MapPool> GetMapPoolsBySeason(SQLInteraction interactionHelper, int season)
    {
        string query = "SELECT * FROM map_pools WHERE Season = " + season;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);

        return mapPools;
    }
    
    public static List<DataTypes.Map> GetMaps(SQLInteraction interactionHelper)
    {
        string query = "SELECT * FROM maps";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }
    
    public static List<DataTypes.Map> GetMapsInMapPool(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "SELECT * FROM maps WHERE MPID = " + MapPoolID;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> mapRes = ReadMaps(reader);

        return mapRes;
    }
    
    public static DataTypes.Map GetMapById(SQLInteraction interactionHelper, ulong MID)
    {
        string query = "SELECT * FROM maps WHERE MapID = " + MID + " LIMIT 1";
        var reader = interactionHelper.GetReader(query);
        DataTypes.Map mapRes = ReadSingleMap(reader);

        return mapRes;
    }

    public static List<DataTypes.Map> GetMapsByName(SQLInteraction interactionHelper, string searchTerm)
    {
        string query = "SELECT * FROM maps WHERE MapName LIKE '%" + searchTerm + "%'";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }
    
    public static List<DataTypes.Map> GetMapsBySeason(SQLInteraction interactionHelper, int season)
    {
        string query = "SELECT * FROM maps WHERE Season = " + season;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> maps = ReadMaps(reader);

        return maps;
    }
    
    public static void UpdateMap(SQLInteraction interactionHelper, DataTypes.Map map)
    {
        string query = "UPDATE maps SET BSMapID = '" + map.BSMapID + "', MapName = '" + map.MapName + "', MapImageLink = '" + map.MapImageLink + "', MapPoolID = " + map.MapPoolID + ", Season = " + map.Season + " WHERE MID = " + map.MID;
        interactionHelper.SendCommand(query);
    }
    
    public static void AddMap(SQLInteraction interactionHelper, DataTypes.Map map)
    {
        string query = "INSERT INTO maps (BSMapID, MapName, MapImageLink, MPID, Season) VALUES ('" + map.BSMapID + "', '" + map.MapName + "', '" + map.MapImageLink + "', " + map.MapPoolID + ", " + map.Season + ")";
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteMap(SQLInteraction interactionHelper, ulong MID)
    {
        string query = "DELETE FROM maps WHERE MapID = " + MID;
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