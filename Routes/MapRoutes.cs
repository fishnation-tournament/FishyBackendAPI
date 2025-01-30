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
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.Map> maps = MapTools.GetMaps(interactionHelper);
            conn.Close();
            return maps;
        }).WithName("GetMaps").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapBySeason/{season}", (int season) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.Map> maps = MapTools.GetMapsBySeason(interactionHelper, season);
            conn.Close();
            return maps;
        }).WithName("GetMapsBySeason").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapById/{MID}", (ulong MID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.Map map = MapTools.GetMapById(interactionHelper, MID);
            conn.Close();
            return map;
        }).WithName("GetMapById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapByName/{searchTerm}", (string searchTerm) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.Map> maps = MapTools.GetMapsByName(interactionHelper, searchTerm);
            conn.Close();
            return maps;
        }).WithName("SearchMapsByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPools", () =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPools(interactionHelper);
            conn.Close();
            return mapPools;
        }).WithName("GetMapPools").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolById/{MapPoolID}", (ulong MapPoolID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.MapPool mapPool = MapTools.GetMapPoolById(interactionHelper, MapPoolID);
            conn.Close();
            return mapPool;
        }).WithName("GetMapPoolById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapPoolByName/{searchTerm}", (string searchTerm) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolByName(interactionHelper, searchTerm);
            conn.Close();
            return mapPools;
        }).WithName("SearchMapPoolByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolsBySeason/{season}", (int season) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolsBySeason(interactionHelper, season);
            conn.Close();
            return mapPools;
        }).WithName("GetMapPoolsBySeason").WithOpenApi();
        
        endpoints.MapPost("/Maps/AddMapPool", async (FrontFacingDataTypes.FrontFacingMapPool mapPool) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.MapPool newMapPool = MapTools.AddMapPool(interactionHelper, mapPool);
            conn.Close();
            return Results.Created($"/Maps/GetMapPoolById/{newMapPool.MapPoolID}", newMapPool);
        }).WithName("AddMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPost("/Maps/AddMap", async (FrontFacingDataTypes.FrontFacingMap map) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            DataTypes.Map newMap = MapTools.AddMap(interactionHelper, map);
            conn.Close();
            return Results.Created($"/Maps/GetMapById/{newMap.MID}", newMap);
        }).WithName("AddMap").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapPut("/Maps/UpdateMapPool", async (DataTypes.MapPool mapPool) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            MapTools.UpdateMapPool(interactionHelper, mapPool);
            conn.Close();
            return Results.Created($"/Maps/GetMapPoolById/{mapPool.MapPoolID}", mapPool);
        }).WithName("UpdateMapPool").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapPut("/Maps/UpdateMap", async (DataTypes.Map map) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            MapTools.UpdateMap(interactionHelper, map);
            conn.Close();
            return Results.Created($"/Maps/GetMapById/{map.MID}", map);
        }).WithName("UpdateMap").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapDelete("/Maps/DeleteMapPool/{MapPoolID}", (ulong MapPoolID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            MapTools.DeleteMapPool(interactionHelper, MapPoolID);
            conn.Close();
            return Results.NoContent();
        }).WithName("DeleteMapPool").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapDelete("/Maps/DeleteMap/{MID}", (ulong MID) =>
        {
            DBConnection conn = connManager.IssueConnection();
            SQLInteraction interactionHelper = new SQLInteraction(conn.Connection);
            MapTools.DeleteMap(interactionHelper, MID);
            conn.Close();
            return Results.NoContent();
        }).WithName("DeleteMap").WithOpenApi().RequireAuthorization("Organizer");
    }
}