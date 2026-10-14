using System.Security.Claims;
using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;
namespace FishyAPI.Routes;

public static class MapRoutes
{
    /// <summary>Private map pools, and the maps in them, are only visible with MapPoolsViewPrivate.</summary>
    private static bool CanViewPrivatePools(HttpContext context) => context.HasPermission(Permission.MapPoolsViewPrivate);

    public static void MapMapRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/Maps/GetMaps", (HttpContext context) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Map> maps = MapTools.GetMaps(conn, CanViewPrivatePools(context));
            return maps;
        }).WithName("GetMaps").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapBySeason/{season}", (HttpContext context, int season) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Map> maps = MapTools.GetMapsBySeason(conn, season, CanViewPrivatePools(context));
            return maps;
        }).WithName("GetMapsBySeason").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapById/{MID}", (HttpContext context, ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Map map = MapTools.GetMapById(conn, MID, CanViewPrivatePools(context));
            return map;
        }).WithName("GetMapById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapByName/{searchTerm}", (HttpContext context, string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<Map> maps = MapTools.GetMapsByName(conn, searchTerm, CanViewPrivatePools(context));
            return maps;
        }).WithName("SearchMapsByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPools", (HttpContext context) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<MapPool> mapPools = MapTools.GetMapPools(conn, CanViewPrivatePools(context));
            return mapPools;
        }).WithName("GetMapPools").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolById/{MapPoolID}", (HttpContext context, ulong MapPoolID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapPool mapPool = MapTools.GetMapPoolById(conn, MapPoolID, CanViewPrivatePools(context));
            return mapPool;
        }).WithName("GetMapPoolById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapPoolByName/{searchTerm}", (HttpContext context, string searchTerm) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<MapPool> mapPools = MapTools.GetMapPoolByName(conn, searchTerm, CanViewPrivatePools(context));
            return mapPools;
        }).WithName("SearchMapPoolByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolsBySeason/{season}", (HttpContext context, int season) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<MapPool> mapPools = MapTools.GetMapPoolsBySeason(conn, season, CanViewPrivatePools(context));
            return mapPools;
        }).WithName("GetMapPoolsBySeason").WithOpenApi();

        endpoints.MapPost("/Maps/AddMapPool", async (FrontFacingMapPool mapPool) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapPool newMapPool = MapTools.AddMapPool(conn, mapPool);
            return Results.Created($"/Maps/GetMapPoolById/{newMapPool.MapPoolID}", newMapPool);
        }).WithName("AddMapPool").WithOpenApi().RequirePermission(Permission.MapPoolsManage);

        endpoints.MapPost("/Maps/AddMap", async (FrontFacingMap map) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            Map newMap = MapTools.AddMap(conn, map);
            return Results.Created($"/Maps/GetMapById/{newMap.MID}", newMap);
        }).WithName("AddMap").WithOpenApi().RequirePermission(Permission.MapsManage);

        endpoints.MapPut("/Maps/UpdateMapPool", async (MapPool mapPool) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.UpdateMapPool(conn, mapPool);
            return Results.Created($"/Maps/GetMapPoolById/{mapPool.MapPoolID}", mapPool);
        }).WithName("UpdateMapPool").WithOpenApi().RequirePermission(Permission.MapPoolsManage);

        endpoints.MapPut("/Maps/UpdateMap", async (Map map) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.UpdateMap(conn, map);
            return Results.Created($"/Maps/GetMapById/{map.MID}", map);
        }).WithName("UpdateMap").WithOpenApi().RequirePermission(Permission.MapsManage);

        endpoints.MapDelete("/Maps/DeleteMapPool/{MapPoolID}", (ulong MapPoolID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.DeleteMapPool(conn, MapPoolID);
            return Results.NoContent();
        }).WithName("DeleteMapPool").WithOpenApi().RequirePermission(Permission.MapPoolsManage);

        endpoints.MapDelete("/Maps/DeleteMap/{MID}", (ulong MID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            MapTools.DeleteMap(conn, MID);
            return Results.NoContent();
        }).WithName("DeleteMap").WithOpenApi().RequirePermission(Permission.MapsManage);
    }
}
