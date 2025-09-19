using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Movement_Home_Task.Models;

namespace Movement_Home_Task.Persistence
{
    public class CacheStorage : IDataStorage
    {
        private readonly IDistributedCache cache;
        public CacheStorage(IDistributedCache _cache)
        {
            this.cache = _cache;
        }

        /// <summary>
        ///     Retrieves a User object from the distributed cache using the provided identifier (ID or email).
        ///     Throws ArgumentNullException if the identifier is null or empty.
        ///     If the user is found, refreshes the cache expiration and returns the User; otherwise, returns null.
        /// </summary>
        /// <param name="id"></param>
        public async Task<User?> GetUserById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Identifier required for 'CacheStorage -> GetUserById'");
            }

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

        /// <summary>
        ///     Stores a User object in the distributed cache using the user's ID as the key.
        ///     Throws ArgumentNullException if the user is null.
        ///     The cached entry expires after 10 minutes.
        /// </summary>
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
