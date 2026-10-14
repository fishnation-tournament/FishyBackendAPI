using System.Security.Claims;
using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
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

            if (string.IsNullOrWhiteSpace(request.TokenName) || string.IsNullOrWhiteSpace(request.Role))
                return Results.BadRequest("TokenName and Role are required.");

            // A token acts as one of the issuer's own roles, and is further limited to what the issuer holds at use time.
            UserAccess access = context.GetAccess();
            string? scopedRole = access.Roles.FirstOrDefault(r => string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
            if (scopedRole == null)
                return Results.BadRequest($"Role must be one of your roles: {string.Join(", ", access.Roles)}.");

            string tokenId = Guid.NewGuid().ToString();
            DateTime jwtExpiry = request.ExpiresAt ?? NoExpiry;

            using MySqlConnection conn = connManager.IssueConnection();
            ApiToken issued = ApiTokenTools.IssueApiToken(conn, uid, tokenId, request.TokenName, scopedRole, request.ExpiresAt);

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
        }).WithName("IssueApiToken").WithOpenApi().RequirePermission(Permission.ApiTokensManageOwn);

        app.MapGet("/Auth/ApiTokens", (HttpContext context) =>
        {
            var uidClaim = context.User.FindFirstValue("uid");
            
            Console.WriteLine($"UID claim for GetApiTokens: {uidClaim}");
            if (uidClaim == null || !ulong.TryParse(uidClaim, out ulong uid))
                return Results.Unauthorized();

            using MySqlConnection conn = connManager.IssueConnection();
            List<ApiToken> tokens = ApiTokenTools.GetApiTokensByOwner(conn, uid);
            return Results.Ok(tokens);
        }).WithName("GetApiTokens").WithOpenApi().RequirePermission(Permission.ApiTokensManageOwn);

        app.MapGet("/Auth/ApiTokens/All", (
            ulong? ownerUid,
            string? role,
            bool? isRevoked,
            int page = 1,
            int pageSize = 50) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            using MySqlConnection conn = connManager.IssueConnection();
            var (tokens, totalCount) = ApiTokenTools.GetAllApiTokens(conn, ownerUid, role, isRevoked, page, pageSize);
            return Results.Ok(new { TotalCount = totalCount, Page = page, PageSize = pageSize, Tokens = tokens });
        }).WithName("GetAllApiTokens").WithOpenApi().RequirePermission(Permission.ApiTokensManageAll);

        app.MapDelete("/Auth/ApiTokens/{tokenId}", (HttpContext context, string tokenId) =>
        {
            var uidClaim = context.User.FindFirstValue("uid");
            if (uidClaim == null || !ulong.TryParse(uidClaim, out ulong uid))
                return Results.Unauthorized();

            bool canRevokeAny = context.HasPermission(Permission.ApiTokensManageAll);

            using MySqlConnection conn = connManager.IssueConnection();
            bool revoked = ApiTokenTools.RevokeApiToken(conn, tokenId, uid, canRevokeAny);

            return revoked ? Results.NoContent() : Results.NotFound();
        }).WithName("RevokeApiToken").WithOpenApi().RequirePermission(Permission.ApiTokensManageOwn);
    }
}
