using Microsoft.IdentityModel.Tokens;
using System.Text;
using Movement_Home_Task.Models;
using Movement_Home_Task.Persistence;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Movement_Home_Task.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration config;
        private readonly IDataStorage cache;
        private readonly IDataStorage file;
        private readonly IDataStorage db;

        public AuthService(IConfiguration _config, IStorageFactory factory)
        {
            config = _config;
            cache = factory.CreateStorage(StorageType.Cache);
            file = factory.CreateStorage(StorageType.File);
            db = factory.CreateStorage(StorageType.Database);
        }

        public async Task<(User? user, string? jwt)> TokenRetrie(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return (null, null);

            var user =
                await cache.GetUserById(id) ?? 
                await file.GetUserById(id) ??
                await db.GetUserById(id) ??
                null;

            if (user is null) return (null, null);

            var jwt = CreateJwt(user);

            if(string.IsNullOrEmpty(jwt)) return (null, null);

            return (user, jwt);
        }

        private string? CreateJwt(User user)
        {
            var issuer = config["Jwt:Issuer"]!;
            var audience = config["Jwt:Audience"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id!),
                new(ClaimTypes.Role, (user.Role ?? Role.User).ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new("created_at", user.CreatedAt.ToUniversalTime().ToString("O")) // ISO-8601
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(100),
                signingCredentials:creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
