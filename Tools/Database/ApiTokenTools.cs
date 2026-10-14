using MySql.Data.MySqlClient;
using FishyAPI.Tools.DataTypes;

namespace FishyAPI.Tools;

public static class ApiTokenTools
{
    public static ApiToken IssueApiToken(MySqlConnection conn, ulong ownerUID, string tokenId, string name, string role, DateTime? expiresAt)
    {
        MySqlCommand cmd = new MySqlCommand("""
            INSERT INTO api_tokens (TokenID, OwnerUID, TokenName, Role, CreatedAt, ExpiresAt, IsRevoked)
            VALUES (@TokenID, @OwnerUID, @TokenName, @Role, @CreatedAt, @ExpiresAt, 0)
            """, conn);
        cmd.Parameters.AddWithValue("@TokenID", tokenId);
        cmd.Parameters.AddWithValue("@OwnerUID", ownerUID);
        cmd.Parameters.AddWithValue("@TokenName", name);
        cmd.Parameters.AddWithValue("@Role", role);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt.HasValue ? expiresAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : (object)DBNull.Value);
        cmd.ExecuteNonQuery();

        return GetApiToken(conn, tokenId)!.Value;
    }

    public static List<ApiToken> GetApiTokensByOwner(MySqlConnection conn, ulong ownerUID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT TokenID, OwnerUID, TokenName, Role, CreatedAt, ExpiresAt, IsRevoked FROM api_tokens WHERE OwnerUID = @OwnerUID",
            conn);
        cmd.Parameters.AddWithValue("@OwnerUID", ownerUID);

        using MySqlDataReader reader = cmd.ExecuteReader();
        List<ApiToken> tokens = new();
        while (reader.Read())
            tokens.Add(new ApiToken(reader));
        return tokens;
    }

    public static ApiToken? GetApiToken(MySqlConnection conn, string tokenId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT TokenID, OwnerUID, TokenName, Role, CreatedAt, ExpiresAt, IsRevoked FROM api_tokens WHERE TokenID = @TokenID",
            conn);
        cmd.Parameters.AddWithValue("@TokenID", tokenId);

        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new ApiToken(reader);
    }

    public static bool RevokeApiToken(MySqlConnection conn, string tokenId, ulong requestingUID, bool isAdmin)
    {
        string query = isAdmin
            ? "UPDATE api_tokens SET IsRevoked = 1 WHERE TokenID = @TokenID"
            : "UPDATE api_tokens SET IsRevoked = 1 WHERE TokenID = @TokenID AND OwnerUID = @OwnerUID";

        MySqlCommand cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@TokenID", tokenId);
        if (!isAdmin) cmd.Parameters.AddWithValue("@OwnerUID", requestingUID);

        return cmd.ExecuteNonQuery() > 0;
    }

    public static (List<ApiToken> Tokens, int TotalCount) GetAllApiTokens(
        MySqlConnection conn,
        ulong? ownerUID,
        string? role,
        bool? isRevoked,
        int page,
        int pageSize)
    {
        var conditions = new List<string>();
        MySqlCommand cmd = new MySqlCommand();
        cmd.Connection = conn;

        if (ownerUID.HasValue)
        {
            conditions.Add("OwnerUID = @OwnerUID");
            cmd.Parameters.AddWithValue("@OwnerUID", ownerUID.Value);
        }
        if (role != null)
        {
            conditions.Add("Role = @Role");
            cmd.Parameters.AddWithValue("@Role", role);
        }
        if (isRevoked.HasValue)
        {
            conditions.Add("IsRevoked = @IsRevoked");
            cmd.Parameters.AddWithValue("@IsRevoked", isRevoked.Value ? 1 : 0);
        }

        string where = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

        cmd.CommandText = $"SELECT COUNT(*) FROM api_tokens {where}";
        int totalCount = Convert.ToInt32(cmd.ExecuteScalar());

        int offset = (page - 1) * pageSize;
        cmd.CommandText = $"SELECT TokenID, OwnerUID, TokenName, Role, CreatedAt, ExpiresAt, IsRevoked FROM api_tokens {where} ORDER BY CreatedAt DESC LIMIT @PageSize OFFSET @Offset";
        cmd.Parameters.AddWithValue("@PageSize", pageSize);
        cmd.Parameters.AddWithValue("@Offset", offset);

        using MySqlDataReader reader = cmd.ExecuteReader();
        List<ApiToken> tokens = new();
        while (reader.Read())
            tokens.Add(new ApiToken(reader));

        return (tokens, totalCount);
    }

    public static bool IsTokenRevoked(MySqlConnection conn, string tokenId)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT IsRevoked FROM api_tokens WHERE TokenID = @TokenID",
            conn);
        cmd.Parameters.AddWithValue("@TokenID", tokenId);
        object? result = cmd.ExecuteScalar();

        // Treat missing token as revoked (fail safe)
        if (result == null) return true;
        return Convert.ToBoolean(result);
    }
}
