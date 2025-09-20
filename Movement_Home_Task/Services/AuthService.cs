using Microsoft.IdentityModel.Tokens;
using System.Text;
using Movement_Home_Task.Models;
using Movement_Home_Task.Persistence;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using StackExchange.Redis;

namespace Movement_Home_Task.Services
{
    /// <summary>
    /// Authentication service that locates a user across storage layers (cache → file → database)
    /// and issues a signed JWT for that user. Implements read-through/backfill behavior so that
    /// successful lookups populate faster layers.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IConfiguration config;
        private readonly IDataStorage cache;
        private readonly IDataStorage file;
        private readonly IDataStorage db;

        /// <summary>
        /// Initializes a new <see cref="AuthService"/> using application configuration and the storage factory.
        /// </summary>
        /// <param name="_config">Application configuration (reads Jwt:Issuer,Jwt:Audience, Jwt:Key).</param>
        /// <param name="factory">Factory that creates 'IDataStorage' instances for cache, file, and database.</param>
        public AuthService(IConfiguration _config, IStorageFactory factory)
        {
            config = _config;
            cache = factory.CreateStorage(StorageType.Cache);
            file = factory.CreateStorage(StorageType.File);
            db = factory.CreateStorage(StorageType.Database);
        }

        /// <summary>
        /// Retrieves a user by id and, if found, generates a JWT for that user.
        /// </summary>
        /// <param name="id">User identifier to authenticate.</param>
        /// <returns>
        /// A tuple of (<see cref="User"/> user, <see cref="string"/> jwt).
        /// Returns (<c>null</c>, <c>null</c>) when the id is missing/invalid, the user is not found,
        /// or a token cannot be created.
        /// </returns>
        /// <remarks>
        /// Lookup path is cache → file → database (with backfilling on hits in slower layers).
        /// The JWT includes <c>sub</c>, <c>role</c>, <c>iat</c>, and <c>created_at</c> claims.
        /// </remarks>
        public async Task<(User? user, string? jwt)> TokenRetrie(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return (null, null);

            var user = await FindUser(id);

            if (user is null) return (null, null);

            var jwt = CreateJwt(user);

            if(string.IsNullOrEmpty(jwt)) return (null, null);

            return (user, jwt);
        }

        /// <summary>
        /// Finds a user by id using a multi-layer read strategy (cache → file → database),
        /// and backfills faster layers when the user is found in a slower one.
        /// </summary>
        /// <param name="id">User identifier to look up.</param>
        /// <returns>
        /// The 'User' if found; otherwise null (including on unexpected errors).
        /// </returns>
        private async Task<User?> FindUser(string id)
        {
            try
            {
                var user = await cache.GetUserById(id);
                if (user != null) return user;

                user = await file.GetUserById(id);
                if (user != null)
                {
                    await cache.StoreUser(user);
                    return user;
                }

                user = await db.GetUserById(id);
                if (user != null)
                {
                    await Task.WhenAll(cache.StoreUser(user), file.StoreUser(user));
                }

                return user;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a signed JWT for the specified user using HMAC-SHA256 and values from configuration.
        /// </summary>
        /// <param name="user">The authenticated user for whom the token is created.</param>
        /// <returns>
        /// A compact JWT string if creation succeeds; otherwise null.
        /// </returns>
        /// <remarks>
        /// Reads Jwt:Issuer, Jwt:Audience, and Jwt:Key from configuration.
        /// Sets claims:
        /// <list type="bullet">
        ///   - user id
        ///   - user's role
        ///   - issued-at (Unix seconds)
        ///   - created_at: user creation timestamp (ISO-8601, UTC)
        /// </list>
        /// The token expiry is currently set to <c>UtcNow + 100 days</c>.
        /// </remarks>
        private string? CreateJwt(User user)
        {
            var issuer = config["Jwt:Issuer"]!;
            var audience = config["Jwt:Audience"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id!),
                new(ClaimTypes.Role, user.Role.ToString()!),
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
