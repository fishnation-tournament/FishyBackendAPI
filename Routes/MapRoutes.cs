using FishyAPI.Tools;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;
namespace FishyAPI.Routes;

public static class MapRoutes
{
    public static void MapMapRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Maps/GetMaps", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.Map> maps = MapTools.GetMaps(conn);
            return maps;
        }).WithName("GetMaps").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapBySeason/{season}", (int season) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.Map> maps = MapTools.GetMapsBySeason(conn, season);
            return maps;
        }).WithName("GetMapsBySeason").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapById/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.Map map = MapTools.GetMapById(conn, MID);
            return map;
        }).WithName("GetMapById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapByName/{searchTerm}", (string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.Map> maps = MapTools.GetMapsByName(conn, searchTerm);
            return maps;
        }).WithName("SearchMapsByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPools", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPools(conn);
            return mapPools;
        }).WithName("GetMapPools").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolById/{MapPoolID}", (ulong MapPoolID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.MapPool mapPool = MapTools.GetMapPoolById(conn, MapPoolID);
            return mapPool;
        }).WithName("GetMapPoolById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapPoolByName/{searchTerm}", (string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolByName(conn, searchTerm);
            return mapPools;
        }).WithName("SearchMapPoolByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolsBySeason/{season}", (int season) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolsBySeason(conn, season);
            return mapPools;
        }).WithName("GetMapPoolsBySeason").WithOpenApi();

        endpoints.MapPost("/Maps/AddMapPool", async (FrontFacingDataTypes.FrontFacingMapPool mapPool) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.MapPool newMapPool = MapTools.AddMapPool(conn, mapPool);
            return Results.Created($"/Maps/GetMapPoolById/{newMapPool.MapPoolID}", newMapPool);
        }).WithName("AddMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPost("/Maps/AddMap", async (FrontFacingDataTypes.FrontFacingMap map) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            DataTypes.Map newMap = MapTools.AddMap(conn, map);
            return Results.Created($"/Maps/GetMapById/{newMap.MID}", newMap);
        }).WithName("AddMap").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Maps/UpdateMapPool", async (DataTypes.MapPool mapPool) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.UpdateMapPool(conn, mapPool);
            return Results.Created($"/Maps/GetMapPoolById/{mapPool.MapPoolID}", mapPool);
        }).WithName("UpdateMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPut("/Maps/UpdateMap", async (DataTypes.Map map) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.UpdateMap(conn, map);
            return Results.Created($"/Maps/GetMapById/{map.MID}", map);
        }).WithName("UpdateMap").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Maps/DeleteMapPool/{MapPoolID}", (ulong MapPoolID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.DeleteMapPool(conn, MapPoolID);
            return Results.NoContent();
        }).WithName("DeleteMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapDelete("/Maps/DeleteMap/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.DeleteMap(conn, MID);
            return Results.NoContent();
        }).WithName("DeleteMap").WithOpenApi().RequireAuthorization("Organizer");
    }
}