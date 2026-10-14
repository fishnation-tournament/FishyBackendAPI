using FishyAPI.Tools;
using FishyAPI.Tools.Authorization;
using FishyAPI.Tools.DBInteractions;
using MySql.Data.MySqlClient;

namespace FishyAPI.Routes;

public static class AuditLogRoutes
{
    public static void MapAuditLogRoutes(WebApplication app, ConnectionManager connManager)
    {
        app.MapGet("/AuditLog/Search", (
            string? actionType,
            string? actionContext,
            string? role,
            ulong? actorUID,
            DateTime? from,
            DateTime? to,
            int page = 1,
            int pageSize = 50) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            using MySqlConnection conn = connManager.IssueConnection();
            var (logs, totalCount) = AuditLogTools.SearchLogs(
                conn, actionType, actionContext, role, actorUID, from, to, page, pageSize);

            return Results.Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Logs = logs
            });
        }).WithName("SearchAuditLog").WithOpenApi().RequirePermission(Permission.AuditLogRead);
    }
}
