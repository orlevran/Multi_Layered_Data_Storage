using MongoDB.Bson;
using Movement_Home_Task.Models;
using Movement_Home_Task.Models.DTOs;
using Movement_Home_Task.Persistence;

namespace Movement_Home_Task.Services
{
    /// <summary>
    /// Orchestrates user operations across multiple storage layers (cache → file → database).
    /// Implements read-through caching and write-through replication to keep layers in sync.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IDataStorage cache;
        private readonly IDataStorage file;
        private readonly IDataStorage database;

        /// <summary>
        /// Initializes the service with concrete storage implementations using a factory.
        /// </summary>
        /// <param name="factory">
        /// Storage factory that creates and operates the cache, file, and database with 'IDataStorage' instances.
        /// </param>
        public UserService(IStorageFactory factory)
        {
            cache = factory.CreateStorage(StorageType.Cache);
            file = factory.CreateStorage(StorageType.File);
            database = factory.CreateStorage(StorageType.Database);
        }

        /// <summary>
        /// Edits an existing user by id and persists the change to all storage layers (cache, file, database).
        /// </summary>
        /// <param name="id">User identifier to edit.</param>
        /// <param name="request">Edit payload
        /// <returns>
        /// The updated 'User' if the user exists; otherwise null.
        /// </returns>
        /// <remarks>
        /// Workflow: fetch the user. If found, update fields
        /// and perform a write-through to all storages in parallel.
        /// </remarks>
        public async Task<User?> EditUser(string id, EditRequest request)
        {
            var user = await cache.GetUserById(id) ??
                       await file.GetUserById(id) ??
                       await database.GetUserById(id) ?? null;

            if (user == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                user.Description = request.Description;
            }

            await Task.WhenAll(
                cache.StoreUser(user),
                file.StoreUser(user),
                database.StoreUser(user)
            );

            return user;
        }

        /// <summary>
        /// Retrieves a user by id using a multi-layer read strategy: cache → file → database.
        /// </summary>
        /// <param name="id">User identifier to look up.</param>
        /// <returns>
        /// The located 'User'; otherwise null (if not found or on unexpected errors).
        /// </returns>
        /// <remarks>
        /// - If found in cache, returns immediately.<br/>
        /// - If found in file, returns and backfills cache.<br/>
        /// - If found in database, returns and backfills cache and file in parallel.
        /// </remarks>
        public async Task<User?> GetUser(string id)
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

                user = await database.GetUserById(id);
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
        /// Creates a new user and writes it to all storage layers (cache, file, database).
        /// </summary>
        /// <param name="request">Registration payload containing role and description.</param>
        /// <returns>
        /// The newly created 'User'; otherwise null if validation fails or on errors.
        /// </returns>
        /// <remarks>
        /// - Validates basic input (non-empty role/description).
        /// - Maps the textual role to 'Role.Admin' when equals "admin" (case-insensitive); otherwise 'Role.User'.
        /// - Generates a new Mongo-style Id and sets 'User.CreatedAt' to current UTC.
        /// - Persists to all storages in parallel to keep layers consistent.
        /// </remarks>
        public async Task<User?> CreateUser(RegisterRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Role) || string.IsNullOrEmpty(request.Description))
                return null;

            var role = request.Role.Equals("admin", StringComparison.OrdinalIgnoreCase) ? Role.Admin : Role.User;

            try
            {
                var user = new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Role = role,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                await Task.WhenAll(cache.StoreUser(user), file.StoreUser(user), database.StoreUser(user));

                return user;
            }
            catch
            {
                return null;
            }
            
        }
    }
}
