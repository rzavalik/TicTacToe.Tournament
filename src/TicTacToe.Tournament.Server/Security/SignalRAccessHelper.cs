using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TicTacToe.Tournament.Server.Security;

public class SignalRAccessHelper
{
    public static string GenerateSignalRAccessToken(
        string endpoint,
        string accessKey,
        string hubName,
        string userId,
        Guid? tournamentId = null,
        TimeSpan? lifetime = null)
    {
        var audience = $"{endpoint}/client/?hub={hubName.ToLower()}";
        var expiry = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(1));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(accessKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Sub, userId),       
        };

        if (tournamentId.HasValue)
        {
            claims = claims
                .Append(new Claim("tournamentId", tournamentId.ToString()))
                .ToArray();
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Expires = expiry,
            Claims = claims.ToDictionary(c => c.Type, c => (object)c.Value),
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
