using MongoDB.Bson;
using Movement_Home_Task.Models;
using Movement_Home_Task.Models.DTOs;
using Movement_Home_Task.Persistence;

namespace Movement_Home_Task.Services
{
    public class UserService : IUserService
    {
        private readonly IDataStorage cache;
        private readonly IDataStorage file;
        private readonly IDataStorage database;

        public UserService(IStorageFactory factory)
        {
            cache = factory.CreateStorage(StorageType.Cache);
            file = factory.CreateStorage(StorageType.File);
            database = factory.CreateStorage(StorageType.Database);
        }

        public async Task<User?> EditUser(string id, EditRequest request)
        {
            var user = await GetUser(id);
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
