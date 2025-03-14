﻿using MySql.Data.MySqlClient;
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
    
    public static DataTypes.Map AddMap(SQLInteraction interactionHelper, FrontFacingDataTypes.FrontFacingMap frontFacingMap) //POST
    {
        string query = "'INSERT INTO maps (BSMapID, MapName, MapImageLink, MPID, Season, Difficulty) VALUES ('" + frontFacingMap.BSMapID + "', '" + frontFacingMap.MapName + "', '" + frontFacingMap.MapImageLink + "', " + frontFacingMap.MapPoolID + ", " + frontFacingMap.Season + ", " + frontFacingMap.Difficulty + ")";
        interactionHelper.SendCommand(query);
        
        string getIdQuery = "SELECT LAST_INSERT_ID() FROM maps";
        var reader = interactionHelper.GetReader(getIdQuery);
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
        reader.Close();
        return mapOut;
    }
    
    public static void UpdateMap(SQLInteraction interactionHelper, DataTypes.Map map) //PUT
    {
        string query = "'UPDATE maps SET BSMapID = '" + map.BSMapID + "', MapName = '" + map.MapName + "', MapImageLink = '" + map.MapImageLink + "', MPID = " + map.MapPoolID + ", Season = " + map.Season + ", Difficulty = '" + map.Difficulty + "' WHERE MapID = " + map.MID;
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteMap(SQLInteraction interactionHelper, ulong MID) //DELETE
    {
        string query = "DELETE FROM maps WHERE MapID = " + MID;
        interactionHelper.SendCommand(query);
    }
    
    public static DataTypes.MapPool AddMapPool(SQLInteraction interactionHelper, FrontFacingDataTypes.FrontFacingMapPool frontFacingMapPool) //POST
    {
        string query = "'INSERT INTO map_pools (MapPoolName, MapPoolDescription, Season) VALUES ('" + frontFacingMapPool.MapPoolName + "', '" + frontFacingMapPool.MapPoolDescription + "', " + frontFacingMapPool.Season + ")";
        interactionHelper.SendCommand(query);
        
        string getIdQuery = "SELECT LAST_INSERT_ID() FROM map_pools";
        var reader = interactionHelper.GetReader(getIdQuery);
        DataTypes.MapPool mapPoolOut = new DataTypes.MapPool();
        if (reader.Read())
        {
            mapPoolOut.MapPoolID = reader.GetUInt64(0);
            mapPoolOut.MapPoolName = reader.GetString(1);
            mapPoolOut.MapPoolDescription = reader.GetString(2);
            mapPoolOut.Season = reader.GetInt32(3);
        }
        
        reader.Close();
        return mapPoolOut;
    }
    
    public static void UpdateMapPool(SQLInteraction interactionHelper, DataTypes.MapPool mapPool) //PUT
    {
        string query = "'UPDATE map_pools SET MapPoolName = '" + mapPool.MapPoolName + "', MapPoolDescription = '" + mapPool.MapPoolDescription + "', Season = " + mapPool.Season + " WHERE MPID = " + mapPool.MapPoolID;
        interactionHelper.SendCommand(query);
    }
    
    public static void DeleteMapPool(SQLInteraction interactionHelper, ulong MapPoolID) //DELETE
    {
        string query = "DELETE FROM map_pools WHERE MPID = " + MapPoolID;
        interactionHelper.SendCommand(query);
    }
}