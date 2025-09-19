using Movement_Home_Task.Models;

namespace Movement_Home_Task.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// * Returns a (User, jwt) pair if credentials are valid.
        /// * Otherwise returns (null, null).
        /// </summary>
        Task<(User? user, string? jwt)> TokenRetrie(string id);
    }
}
