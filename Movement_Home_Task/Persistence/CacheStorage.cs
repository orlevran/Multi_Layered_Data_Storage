using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Movement_Home_Task.Models;

namespace Movement_Home_Task.Persistence
{
    // <summary>
    /// Distributed cache-backed storage for 'User' entities.
    /// Uses <see 'IDistributedCache' (e.g., Redis) to store and retrieve users
    /// under keys prefixed with 'user:' and a default TTL of 10 minutes.
    /// </summary>
    public class CacheStorage : IDataStorage
    {
        private readonly IDistributedCache cache;
        public CacheStorage(IDistributedCache _cache)
        {
            this.cache = _cache;
        }

        /// <summary>
        /// Retrieves a 'User' from the distributed cache by identifier.
        /// </summary>
        /// <param name="id">The user identifier used to construct the cache key.</param>
        /// <returns>
        /// The cached 'User' if present; otherwise null.
        /// </returns>
        /// <exception 'ArgumentNullException'
        /// Thrown when id is null or empty.
        /// </exception>
        /// <remarks>
        /// - Cache key format: user:{id}
        /// - If the key is not found, returns null without throwing
        /// - Any unexpected exceptions are swallowed and reported as null to
        ///     keep cache lookup non-fatal in read paths.
        /// </remarks>
        public async Task<User?> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Identifier required for 'CacheStorage -> GetUserById'");
            }

            try
            {
                // Construct a cache key using the identifier, prefixed with "user:" to namespace user objects.
                string cacheKey = $"user:{id}";
                // Attempt to retrieve the cached user data as a byte array using the cache key.
                var cachedBytes = await cache.GetAsync(cacheKey);

                // If no cached data is found, return null.
                if (cachedBytes == null) { return null; }

                // If cached data is found, deserialize the byte array into a User object.
                var user = JsonSerializer.Deserialize<User>(cachedBytes);

                return user;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Stores a 'User' in the distributed cache with a 10-minute absolute expiration.
        /// </summary>
        /// <param name="user">The User instance to cache.</param>
        /// <returns>A task that completes when the value has been written to the cache.</returns>
        /// <exception 'ArgumentNullException'
        /// Thrown when user is null.
        /// </exception>
        /// <remarks>
        /// - Cache key format: user:{user.Id}
        /// - Absolute expiration: 10 minutes (no sliding refresh).
        /// - Exceptions are logged to console but not rethrown to keep cache writes non-fatal.
        /// </remarks>
        public async Task StoreUser(User user)
        {

            if(user == null)
            {
                throw new ArgumentNullException(nameof(user), "Valid User object is required");
            }

            try
            {
                // Construct a cache key
                string cacheKey = $"user:{user.Id}";

                // Convert user to a json
                var serializedUser = JsonSerializer.SerializeToUtf8Bytes(user);

                // Define the cache expiration with a new expiration time(10 minutes).
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                // Insert the user into the cache
                await cache.SetAsync(cacheKey, serializedUser, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in 'CacheStorage -> StoreUser': {ex.Message}");
            }
        }
    }
}
