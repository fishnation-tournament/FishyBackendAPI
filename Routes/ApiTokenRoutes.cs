using System.Security.Claims;
using FishyAPI.Tools;
using FishyAPI.Tools.Authentication;
using FishyAPI.Tools.DataTypes;
using FishyAPI.Tools.FrontFacingDataTypes;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class ApiTokenRoutes
{
    // Far-future expiry used in the JWT when the user requests no expiration.
    private static readonly DateTime NoExpiry = new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    public static void MapApiTokenRoutes(WebApplication app, Tokenizer tokenizer, ConnectionManager connManager)
    {
        app.MapPost("/Auth/ApiTokens/Issue", (HttpContext context, IssueApiTokenRequest request) =>
        {
            var uidClaim = context.User.FindFirstValue("uid");
            if (uidClaim == null || !ulong.TryParse(uidClaim, out ulong uid))
                return Results.Unauthorized();

            var issuerRole = context.User.FindFirstValue(ClaimTypes.Role) ?? "Unassigned";
            string scopedRole = ApiTokenTools.ClampRole(request.Role, issuerRole);

            string tokenId = Guid.NewGuid().ToString();
            DateTime jwtExpiry = request.ExpiresAt ?? NoExpiry;

            using MySqlConnection conn = connManager.IssueConnection();
            ApiToken issued = ApiTokenTools.IssueApiToken(conn, uid, tokenId, request.Name, scopedRole, request.ExpiresAt);

            string jwt = tokenizer.GenerateApiToken(uid, scopedRole, jwtExpiry, tokenId);

            return Results.Ok(new IssuedApiTokenResponse
            {
                TokenID = issued.TokenID,
                TokenName = issued.TokenName,
                Role = issued.Role,
                CreatedAt = issued.CreatedAt,
                ExpiresAt = issued.ExpiresAt,
                Token = jwt
            });
        }).WithName("IssueApiToken").WithOpenApi().RequireAuthorization("User");

        app.MapGet("/Auth/ApiTokens", (HttpContext context) =>
        {
            var uidClaim = context.User.FindFirstValue("uid");
            if (uidClaim == null || !ulong.TryParse(uidClaim, out ulong uid))
                return Results.Unauthorized();

            using MySqlConnection conn = connManager.IssueConnection();
            List<ApiToken> tokens = ApiTokenTools.GetApiTokensByOwner(conn, uid);
            return Results.Ok(tokens);
        }).WithName("GetApiTokens").WithOpenApi().RequireAuthorization("User");

        app.MapDelete("/Auth/ApiTokens/{tokenId}", (HttpContext context, string tokenId) =>
        {
            var uidClaim = context.User.FindFirstValue("uid");
            if (uidClaim == null || !ulong.TryParse(uidClaim, out ulong uid))
                return Results.Unauthorized();

            var role = context.User.FindFirstValue(ClaimTypes.Role) ?? "Unassigned";
            bool isAdmin = role == "Admin";

            using MySqlConnection conn = connManager.IssueConnection();
            bool revoked = ApiTokenTools.RevokeApiToken(conn, tokenId, uid, isAdmin);

            return revoked ? Results.NoContent() : Results.NotFound();
        }).WithName("RevokeApiToken").WithOpenApi().RequireAuthorization("User");
    }
}
