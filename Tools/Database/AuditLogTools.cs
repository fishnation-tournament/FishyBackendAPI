using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;

namespace FishyAPI.Tools;

public static class AuditLogTools
{
    public static void InsertLog(
        MySqlConnection conn,
        ulong actorUID,
        string actorRole,
        string actionType,
        string actionContext,
        string? actionDetail,
        string tokenType)
    {
        MySqlCommand cmd = new MySqlCommand("""
            INSERT INTO audit_log (ActorUID, ActorRole, ActionType, ActionContext, ActionDetail, TokenType, Timestamp)
            VALUES (@ActorUID, @ActorRole, @ActionType, @ActionContext, @ActionDetail, @TokenType, @Timestamp)
            """, conn);
        cmd.Parameters.AddWithValue("@ActorUID", actorUID);
        cmd.Parameters.AddWithValue("@ActorRole", actorRole);
        cmd.Parameters.AddWithValue("@ActionType", actionType);
        cmd.Parameters.AddWithValue("@ActionContext", actionContext);
        cmd.Parameters.AddWithValue("@ActionDetail", (object?)actionDetail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TokenType", tokenType);
        cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public static (List<AuditLog> Logs, int TotalCount) SearchLogs(
        MySqlConnection conn,
        string? actionType,
        string? actionContext,
        string? actorRole,
        ulong? actorUID,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize)
    {
        var conditions = new List<string>();
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = conn;

        if (actionType != null)
        {
            conditions.Add("ActionType = @ActionType");
            cmd.Parameters.AddWithValue("@ActionType", actionType.ToUpper());
        }
        if (actionContext != null)
        {
            conditions.Add("ActionContext LIKE @ActionContext");
            cmd.Parameters.AddWithValue("@ActionContext", $"%{actionContext}%");
        }
        if (actorRole != null)
        {
            conditions.Add("ActorRole = @ActorRole");
            cmd.Parameters.AddWithValue("@ActorRole", actorRole);
        }
        if (actorUID.HasValue)
        {
            conditions.Add("ActorUID = @ActorUID");
            cmd.Parameters.AddWithValue("@ActorUID", actorUID.Value);
        }
        if (from.HasValue)
        {
            conditions.Add("Timestamp >= @From");
            cmd.Parameters.AddWithValue("@From", from.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        if (to.HasValue)
        {
            conditions.Add("Timestamp <= @To");
            cmd.Parameters.AddWithValue("@To", to.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        string where = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

        cmd.CommandText = $"SELECT COUNT(*) FROM audit_log {where}";
        int totalCount = Convert.ToInt32(cmd.ExecuteScalar());

        int offset = (page - 1) * pageSize;
        cmd.CommandText = $"""
            SELECT LogID, ActorUID, ActorRole, ActionType, ActionContext, ActionDetail, TokenType, Timestamp
            FROM audit_log {where}
            ORDER BY Timestamp DESC
            LIMIT @PageSize OFFSET @Offset
            """;
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        cmd.Parameters.AddWithValue("@Offset", offset);

        using MySqlDataReader reader = cmd.ExecuteReader();
        List<AuditLog> logs = new();
        while (reader.Read())
            logs.Add(new AuditLog(reader));

        return (logs, totalCount);
    }
}
