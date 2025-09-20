using System.Diagnostics;
using Movement_Home_Task.Models;

namespace Movement_Home_Task.Persistence
{
    /// <summary>
    /// Decorator for IDataStorage that adds structured logging and timing around storage operations.
    /// Useful for observability across cache/file/database layers.
    /// </summary>
    /// <remarks>
    /// This class implements the Decorator pattern: it wraps another
    /// IDataStorage inner and augments behavior
    /// without changing the underlying implementation.
    /// </remarks>
    public sealed class LoggingStorageDecorator : IDataStorage
    {
        private readonly IDataStorage inner;
        private readonly ILogger<LoggingStorageDecorator> logger;
        private readonly string layer; // "cache" | "file" | "database"

        /// <summary>
        /// Initializes a new instance of LoggingStorageDecorator/>.
        /// </summary>
        /// <param name="inner">The underlying IDataStorage to decorate.</param>
        /// <param name="logger">Structured logger used to emit timing and outcome information.</param>
        /// <param name="layer">Logical layer name for log context (e.g., "cache", "file", "database").</param>
        public LoggingStorageDecorator(IDataStorage inner, ILogger<LoggingStorageDecorator> logger, string layer)
        {
            this.inner = inner;
            this.logger = logger;
            this.layer = layer;
        }

        /// <summary>
        /// Retrieves a user by id and logs timing and outcome (hit/miss/failure).
        /// </summary>
        /// <param name="id">The user identifier.</param>
        /// <returns>
        /// The User if found; otherwise null.
        /// </returns>
        /// <remarks>
        /// - Starts a Stopwatch to measure elapsed time.
        /// - On success, logs an information event with the layer, id, whether a user was found, and elapsed milliseconds.
        /// - On failure, logs an error event with the exception and elapsed milliseconds, then rethrows.
        /// </remarks>
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

        /// <summary>
        /// Stores a user and logs timing and outcome (success/failure).
        /// </summary>
        /// <param name="user">The user instance to persist.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        /// <remarks>
        /// - Measures elapsed time with Stopwatch
        /// - On success, logs information with layer, user id, and elapsed time.<br/>
        /// - On failure, logs error with exception and elapsed time, then rethrows.<br/>
        ///     Note: The log template labels elapsed time as ms while the code uses sw.Elapsed.TotalNanoseconds;
        ///     consider switching to sw.Elapsed.TotalMilliseconds for consistency with the label.
        /// </remarks>
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
