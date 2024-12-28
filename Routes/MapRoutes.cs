using FishyAPI.Tools;
namespace FishyAPI.Routes;

public static class MapRoutes
{
    public static void MapMapRoutes(WebApplication endpoints, SQLInteraction interactionHelper)
    {
        endpoints.MapGet("/Maps/GetMaps", () =>
        {
            List<DataTypes.Map> maps = MapTools.GetMaps(interactionHelper);
            return maps;
        }).WithName("GetMaps").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapBySeason/{season}", (int season) =>
        {
            List<DataTypes.Map> maps = MapTools.GetMapsBySeason(interactionHelper, season);
            return maps;
        }).WithName("GetMapsBySeason").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapById/{MID}", (ulong MID) =>
        {
            DataTypes.Map map = MapTools.GetMapById(interactionHelper, MID);
            return map;
        }).WithName("GetMapById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapByName/{searchTerm}", (string searchTerm) =>
        {
            List<DataTypes.Map> maps = MapTools.GetMapsByName(interactionHelper, searchTerm);
            return maps;
        }).WithName("SearchMapsByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPools", () =>
        {
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPools(interactionHelper);
            return mapPools;
        }).WithName("GetMapPools").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolById/{MapPoolID}", (ulong MapPoolID) =>
        {
            DataTypes.MapPool mapPool = MapTools.GetMapPoolById(interactionHelper, MapPoolID);
            return mapPool;
        }).WithName("GetMapPoolById").WithOpenApi();

        endpoints.MapGet("/Maps/SearchMapPoolByName/{searchTerm}", (string searchTerm) =>
        {
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolByName(interactionHelper, searchTerm);
            return mapPools;
        }).WithName("SearchMapPoolByName").WithOpenApi();

        endpoints.MapGet("/Maps/GetMapPoolsBySeason/{season}", (int season) =>
        {
            List<DataTypes.MapPool> mapPools = MapTools.GetMapPoolsBySeason(interactionHelper, season);
            return mapPools;
        }).WithName("GetMapPoolsBySeason").WithOpenApi();
        
        endpoints.MapPost("/Maps/AddMapPool", async (FrontFacingDataTypes.FrontFacingMapPool mapPool) =>
        {
            DataTypes.MapPool newMapPool = MapTools.AddMapPool(interactionHelper, mapPool);
            return Results.Created($"/Maps/GetMapPoolById/{newMapPool.MapPoolID}", newMapPool);
        }).WithName("AddMapPool").WithOpenApi().RequireAuthorization("Organizer");

        endpoints.MapPost("/Maps/AddMap", async (FrontFacingDataTypes.FrontFacingMap map) =>
        {
            DataTypes.Map newMap = MapTools.AddMap(interactionHelper, map);
            return Results.Created($"/Maps/GetMapById/{newMap.MID}", newMap);
        }).WithName("AddMap").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapPut("/Maps/UpdateMapPool", async (DataTypes.MapPool mapPool) =>
        {
            MapTools.UpdateMapPool(interactionHelper, mapPool);
            return Results.Created($"/Maps/GetMapPoolById/{mapPool.MapPoolID}", mapPool);
        }).WithName("UpdateMapPool").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapPut("/Maps/UpdateMap", async (DataTypes.Map map) =>
        {
            MapTools.UpdateMap(interactionHelper, map);
            return Results.Created($"/Maps/GetMapById/{map.MID}", map);
        }).WithName("UpdateMap").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapDelete("/Maps/DeleteMapPool/{MapPoolID}", (ulong MapPoolID) =>
        {
            MapTools.DeleteMapPool(interactionHelper, MapPoolID);
            return Results.NoContent();
        }).WithName("DeleteMapPool").WithOpenApi().RequireAuthorization("Organizer");
        
        endpoints.MapDelete("/Maps/DeleteMap/{MID}", (ulong MID) =>
        {
            MapTools.DeleteMap(interactionHelper, MID);
            return Results.NoContent();
        }).WithName("DeleteMap").WithOpenApi().RequireAuthorization("Organizer");
    }
}