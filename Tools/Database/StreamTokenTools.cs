using FishyAPI.Tools.DataTypes;
using MySql.Data.MySqlClient;

namespace FishyAPI.Tools;

public static class StreamTokenTools
{
    public static List<StreamToken> GetAllStreamTokens(MySqlConnection dbConn)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT UserID, ingress_link_primary, ingress_link_secondary, stream_key FROM stream_tokens",
            dbConn);
        using MySqlDataReader reader = cmd.ExecuteReader();
        List<StreamToken> tokens = new List<StreamToken>();
        while (reader.Read())
            tokens.Add(new StreamToken(reader));
        return tokens;
    }

    public static StreamToken? GetStreamTokenByUserID(MySqlConnection dbConn, int userID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "SELECT UserID, ingress_link_primary, ingress_link_secondary, stream_key FROM stream_tokens WHERE UserID = @UserID LIMIT 1",
            dbConn);
        cmd.Parameters.AddWithValue("@UserID", userID);
        using MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new StreamToken(reader);
    }

    public static StreamToken AddStreamToken(MySqlConnection dbConn, StreamToken token)
    {
        MySqlCommand cmd = new MySqlCommand("""
            INSERT INTO stream_tokens (UserID, ingress_link_primary, ingress_link_secondary, stream_key)
            VALUES (@UserID, @IngressPrimary, @IngressSecondary, @StreamKey)
            """, dbConn);
        cmd.Parameters.AddWithValue("@UserID", token.UserID);
        cmd.Parameters.AddWithValue("@IngressPrimary", token.IngressLinkPrimary);
        cmd.Parameters.AddWithValue("@IngressSecondary", token.IngressLinkSecondary);
        cmd.Parameters.AddWithValue("@StreamKey", token.StreamKey);
        cmd.ExecuteNonQuery();
        return GetStreamTokenByUserID(dbConn, token.UserID)!.Value;
    }

    public static StreamToken? UpdateStreamToken(MySqlConnection dbConn, int userID, string? ingressPrimary, string? ingressSecondary, string? streamKey)
    {
        StreamToken? existing = GetStreamTokenByUserID(dbConn, userID);
        if (!existing.HasValue) return null;

        MySqlCommand cmd = new MySqlCommand("""
            UPDATE stream_tokens
            SET ingress_link_primary = @IngressPrimary,
                ingress_link_secondary = @IngressSecondary,
                stream_key = @StreamKey
            WHERE UserID = @UserID
            """, dbConn);
        cmd.Parameters.AddWithValue("@IngressPrimary", ingressPrimary ?? existing.Value.IngressLinkPrimary);
        cmd.Parameters.AddWithValue("@IngressSecondary", ingressSecondary ?? existing.Value.IngressLinkSecondary);
        cmd.Parameters.AddWithValue("@StreamKey", streamKey ?? existing.Value.StreamKey);
        cmd.Parameters.AddWithValue("@UserID", userID);
        cmd.ExecuteNonQuery();
        return GetStreamTokenByUserID(dbConn, userID);
    }

    public static bool DeleteStreamToken(MySqlConnection dbConn, int userID)
    {
        MySqlCommand cmd = new MySqlCommand(
            "DELETE FROM stream_tokens WHERE UserID = @UserID",
            dbConn);
        cmd.Parameters.AddWithValue("@UserID", userID);
        return cmd.ExecuteNonQuery() > 0;
    }
}
