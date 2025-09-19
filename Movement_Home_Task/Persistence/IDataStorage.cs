using Movement_Home_Task.Models;

namespace Movement_Home_Task.Persistence
{
    public interface IDataStorage
    {
        Task<User?> GetUserById(string id);
        Task StoreUser(User user);
    }
}
