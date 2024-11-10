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
        string query = "SELECT * FROM mappools";
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.MapPool> mapPools = ReadMapPools(interactionHelper, reader);
        return mapPools;
    }
    
    public static DataTypes.MapPool GetMapPoolById(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "SELECT * FROM mappools WHERE MapPoolID = " + MapPoolID + " LIMIT 1";
        var reader = interactionHelper.GetReader(query);
        DataTypes.MapPool mapPoolRes = ReadSingleMapPool(interactionHelper, reader);
        return mapPoolRes;
    }
    
    public static List<DataTypes.Map> GetMapsInMapPool(SQLInteraction interactionHelper, ulong MapPoolID)
    {
        string query = "SELECT * FROM maps WHERE MapPoolID = " + MapPoolID;
        var reader = interactionHelper.GetReader(query);
        List<DataTypes.Map> mapRes = ReadMaps(reader);
        return mapRes;
    }
}