using FishyAPI.Tools;
namespace FishyAPI.Routes;

public class MapRoutes
{
    public static void MapMapRoutes(IEndpointRouteBuilder endpoints, SQLInteraction interactionHelper)
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
    }
}