using System.Diagnostics;
using Movement_Home_Task.Models;

namespace Movement_Home_Task.Persistence
{
    public sealed class LoggingStorageDecorator : IDataStorage
    {
        private readonly IDataStorage inner;
        private readonly ILogger<LoggingStorageDecorator> logger;
        private readonly string layer; // "cache" | "file" | "database"

        public LoggingStorageDecorator(IDataStorage inner, ILogger<LoggingStorageDecorator> logger, string layer)
        {
            this.inner = inner;
            this.logger = logger;
            this.layer = layer;
        }

        public async Task<User?> GetUserById(string id)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var user = await inner.GetUserById(id);
                sw.Stop();

                logger.LogInformation("storage[{Layer}].GetUserById id={Id} found={Found} {Elapsed}ms",
                                        layer, id, user is not null, sw.Elapsed.TotalMilliseconds);

                return user;
            }
            catch(Exception ex)
            {
                sw.Stop();
                logger.LogError(ex, "storage[{Layer}].GetUserById id={Id} failed after {Elapsed}ms",
                                layer, id, sw.Elapsed.TotalMilliseconds);
                throw;
            }
        }

        public async Task StoreUser(User user)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await inner.StoreUser(user);
                sw.Stop();

                logger.LogInformation("storage[{Layer}].StoreUser id={Id} ok {Elapsed}ms",
                                        layer, user.Id ?? null, sw.Elapsed.TotalNanoseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.LogError(ex, "storage[{Layer}].StoreUser id={Id} failed after {Elapsed}ms",
                                layer, user?.Id, sw.Elapsed.TotalMilliseconds);
                throw;
            }
        }
    }
}
