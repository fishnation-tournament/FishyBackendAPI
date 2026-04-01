using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace FishyAPI.Tools.Authentication;

public class Tokenizer
{
    private string secretKey; 
    
    public Tokenizer(string secretKey)
    {
        this.secretKey = secretKey;
    }
    
    public string GenerateToken(DataTypes.User user, string role, DateTime expiry)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UID.ToString()),
            new Claim("uid", user.UID.ToString()),
            new Claim("discord_id", user.DiscordID?.ToString() ?? ""),
            new Claim("google_id", user.GoogleID ?? ""),
            new Claim(ClaimTypes.Role, role),
            new Claim("token_type", "session")
        };

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            "Fishnation",
            "TournamentUser",
            claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateApiToken(ulong uid, string role, DateTime expiry, string tokenId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, uid.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim("uid", uid.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("token_type", "api")
        };

        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            "Fishnation",
            "TournamentUser",
            claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}