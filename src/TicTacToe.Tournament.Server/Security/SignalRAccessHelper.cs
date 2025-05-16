namespace TicTacToe.Tournament.Server.Security
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;

    public class SignalRAccessHelper
    {
        public static string GenerateSignalRAccessToken(
            string signalrConnectionString,
            string hubName,
            string userId,
            Guid? tournamentId = null,
            TimeSpan? lifetime = null)
        {
            var parts = signalrConnectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .ToDictionary(p => p[0], p => p[1]);

            var endpoint = parts["Endpoint"];
            var accessKey = parts["AccessKey"];
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
                    .Append(new Claim("tournamentId", tournamentId.Value.ToString()))
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
}