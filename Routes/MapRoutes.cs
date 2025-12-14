using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;
namespace FishyAPI.Routes;

public static class MapRoutes
{
    public static void MapMapRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Maps/GetMaps", () =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.Map> maps = MapTools.GetMaps(conn.Connection);
            conn.Close();
            return maps;
        }).WithName("GetMaps").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapBySeason/{season}", (int season) =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.Map> maps = MapTools.GetMapsBySeason(conn.Connection, season);
            conn.Close();
            return maps;
        }).WithName("GetMapsBySeason").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapById/{MID}", (ulong MID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            DataTypes.Map map = MapTools.GetMapById(conn.Connection, MID);
            conn.Close();
            return map;
        }).WithName("GetMapById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapByName/{searchTerm}", (string searchTerm) =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.Map> maps = MapTools.GetMapsByName(conn.Connection, searchTerm);
            conn.Close();
            return maps;
        }).WithName("SearchMapsByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPools", () =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPools(conn.Connection);
            conn.Close();
            return mapPools;
        }).WithName("GetMapPools").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolById/{MapPoolID}", (ulong MapPoolID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            DataTypes.MapPool mapPool = MapTools.GetMapPoolById(conn.Connection, MapPoolID);
            conn.Close();
            return mapPool;
        }).WithName("GetMapPoolById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapPoolByName/{searchTerm}", (string searchTerm) =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolByName(conn.Connection, searchTerm);
            conn.Close();
            return mapPools;
        }).WithName("SearchMapPoolByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolsBySeason/{season}", (int season) =>
        {
            DBConnection conn = connManager.IssueConnection();
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolsBySeason(conn.Connection, season);
            conn.Close();
            return mapPools;
        }).WithName("GetMapPoolsBySeason").WithOpenApi();

        endpoints.MapPost("/Maps/AddMapPool", async (FrontFacingDataTypes.FrontFacingMapPool mapPool) =>
        {
            DBConnection conn = connManager.IssueConnection();
            DataTypes.MapPool newMapPool = MapTools.AddMapPool(conn.Connection, mapPool);
            conn.Close();
            return Results.Created($"/Maps/GetMapPoolById/{newMapPool.MapPoolID}", newMapPool);
        }).WithName("AddMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPost("/Maps/AddMap", async (FrontFacingDataTypes.FrontFacingMap map) =>
        {
            DBConnection conn = connManager.IssueConnection();
            DataTypes.Map newMap = MapTools.AddMap(conn.Connection, map);
            conn.Close();
            return Results.Created($"/Maps/GetMapById/{newMap.MID}", newMap);
        }).WithName("AddMap").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Maps/UpdateMapPool", async (DataTypes.MapPool mapPool) =>
        {
            DBConnection conn = connManager.IssueConnection();
            MapTools.UpdateMapPool(conn.Connection, mapPool);
            conn.Close();
            return Results.Created($"/Maps/GetMapPoolById/{mapPool.MapPoolID}", mapPool);
        }).WithName("UpdateMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Maps/UpdateMap", async (DataTypes.Map map) =>
        {
            DBConnection conn = connManager.IssueConnection();
            MapTools.UpdateMap(conn.Connection, map);
            conn.Close();
            return Results.Created($"/Maps/GetMapById/{map.MID}", map);
        }).WithName("UpdateMap").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Maps/DeleteMapPool/{MapPoolID}", (ulong MapPoolID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            MapTools.DeleteMapPool(conn.Connection, MapPoolID);
            conn.Close();
            return Results.NoContent();
        }).WithName("DeleteMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Maps/DeleteMap/{MID}", (ulong MID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            MapTools.DeleteMap(conn.Connection, MID);
            conn.Close();
            return Results.NoContent();
        }).WithName("DeleteMap").WithOpenApi().RequireAuthorization("Organizer");
    }
}