using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.DBInteractions;
using FishyAPI.Tools.FrontFacingDataTypes;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class StreamTokenRoutes
{
    public static void MapStreamTokenRoutes(WebApplication endpoints, ConnectionManager connManager)
    {
        endpoints.MapGet("/StreamTokens/GetAll", () =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            List<StreamToken> tokens = StreamTokenTools.GetAllStreamTokens(conn);
            return Results.Ok(tokens);
        }).WithName("GetAllStreamTokens").WithOpenApi().RequirePermission(Permission.StreamTokensManage);

        endpoints.MapGet("/StreamTokens/GetStreamKey", (HttpContext context) =>
        {
            var uidClaim = context.User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (uidClaim == null)
                return Results.Unauthorized();

            int userID = (int)ulong.Parse(uidClaim.Value);

            using MySqlConnection conn = connManager.IssueConnection();
            StreamToken? token = StreamTokenTools.GetStreamTokenByUserID(conn, userID);
            if (!token.HasValue)
                return Results.NotFound("No stream token found for your account.");

            return Results.Ok(token.Value);
        }).WithName("GetStreamKey").WithOpenApi().RequirePermission(Permission.StreamKeyReadOwn);

        endpoints.MapPost("/StreamTokens/Add", (AddStreamTokenRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();

            if (StreamTokenTools.GetStreamTokenByUserID(conn, request.UserID).HasValue)
                return Results.Conflict("A stream token already exists for this user.");

            StreamToken token = StreamTokenTools.AddStreamToken(conn, new StreamToken
            {
                UserID = request.UserID,
                IngressLinkPrimary = request.IngressLinkPrimary,
                IngressLinkSecondary = request.IngressLinkSecondary,
                StreamKey = request.StreamKey
            });
            return Results.Created($"/StreamTokens/GetStreamKey", token);
        }).WithName("AddStreamToken").WithOpenApi().RequirePermission(Permission.StreamTokensManage);

        endpoints.MapPut("/StreamTokens/Update/{userID}", (int userID, UpdateStreamTokenRequest request) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            StreamToken? updated = StreamTokenTools.UpdateStreamToken(
                conn, userID,
                request.IngressLinkPrimary,
                request.IngressLinkSecondary,
                request.StreamKey);

            if (!updated.HasValue)
                return Results.NotFound($"No stream token found for UserID {userID}.");

            return Results.Ok(updated.Value);
        }).WithName("UpdateStreamToken").WithOpenApi().RequirePermission(Permission.StreamTokensManage);

        endpoints.MapDelete("/StreamTokens/Delete/{userID}", (int userID) =>
        {
            using MySqlConnection conn = connManager.IssueConnection();
            bool deleted = StreamTokenTools.DeleteStreamToken(conn, userID);
            if (!deleted)
                return Results.NotFound($"No stream token found for UserID {userID}.");

            return Results.NoContent();
        }).WithName("DeleteStreamToken").WithOpenApi().RequirePermission(Permission.StreamTokensManage);
    }
}
