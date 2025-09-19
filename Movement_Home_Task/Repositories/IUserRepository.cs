using Movement_Home_Task.Models;

namespace Movement_Home_Task.Repositories
{
    public interface IUserRepository
    {
        Task<User> Get(string id);
        Task Store(User user);
        Task Update(User user);
    }
}
