using Movement_Home_Task.Models;
using Movement_Home_Task.Models.DTOs;

namespace Movement_Home_Task.Services
{
    public interface IUserService
    {
        Task<User?> CreateUser(RegisterRequest request);
        Task<User?> EditUser(string id, EditRequest request);
        Task<User?> GetUser(string id);
    }
}
