using Microsoft.Extensions.Caching.Distributed;
using MongoDB.Driver;
using Movement_Home_Task.Repositories;

namespace Movement_Home_Task.Persistence
{
    /// <summary>
    ///     Factory class for creating IDataStorage instances based on the specified StorageType.
    ///     Supports the following storage mechanisms:
    ///     * Cache (Redis)
    ///     * File (JSON)
    ///     * Database (MongoDB)
    ///     Uses DI for cache storage.
    /// </summary>
    public class StorageFactory : IStorageFactory
    {
        private readonly IDistributedCache cache;
        private readonly IUserRepository repository;
        private readonly ILoggerFactory loggerFactory;

        public StorageFactory(IDistributedCache cache, IUserRepository repository, ILoggerFactory loggerFactory)
        {
            this.cache = cache;
            this.repository = repository;
            this.loggerFactory = loggerFactory;
        }

        public IDataStorage CreateStorage(StorageType type)
        {
            IDataStorage _inner = type switch
            {
                StorageType.Cache => new CacheStorage(cache),
                StorageType.File => new FileStorage(),
                StorageType.Database => new DatabaseStorage(repository),
                _ => throw new ArgumentException("Invalid Storage type")
            };
            /*
            return type switch
            {
                StorageType.Cache => new CacheStorage(cache),
                StorageType.File => new FileStorage(),
                StorageType.Database => new DatabaseStorage(repository),
                _ => throw new ArgumentException("Invalid Storage type")
            };
            */

            var layer = type.ToString().ToLowerInvariant();
            var inner = new LoggingStorageDecorator(_inner, loggerFactory.CreateLogger<LoggingStorageDecorator>(), layer);

            return inner;
        }
    }

    public enum StorageType
    {
        Cache,
        File,
        Database
    }
}
