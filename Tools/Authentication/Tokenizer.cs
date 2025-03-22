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
    
    public string GenerateToken(DataTypes.User user, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UID.ToString()),
            new Claim( JwtRegisteredClaimNames.Sub, user.DiscordID.ToString()),
            new Claim(ClaimTypes.Role, role)
            
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            "Self",
            "Self",
            claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}