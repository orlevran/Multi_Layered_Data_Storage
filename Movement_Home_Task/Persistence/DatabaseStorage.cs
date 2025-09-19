using Movement_Home_Task.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using Movement_Home_Task.Repositories;

namespace Movement_Home_Task.Persistence
{
    public class DatabaseStorage : IDataStorage
    {
        private readonly IUserRepository usersRepository;

        public DatabaseStorage(IUserRepository _repository)
        {
            usersRepository = _repository;
        }

        public async Task<User?> GetUserById(string identifier)
        {
            try
            {
                return await usersRepository.Get(identifier);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in 'DatabaseStorage -> GetUserById': {ex.Message}");
            }
        }

        public async Task StoreUser(User user)
        {
            if(user == null || string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentNullException(nameof(user), "Valid User object required for 'DatabaseStorage -> StoreUser'");
            }
            try
            {
                if (await usersRepository.Get(user.Id) != null)
                {
                    await usersRepository.Update(user);
                }
                else
                {
                    await usersRepository.Store(user);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in 'DatabaseStorage -> StoreUser': {ex.Message}");
            }
        }
    }
}
