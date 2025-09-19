using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Movement_Home_Task.Models;

namespace Movement_Home_Task.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> usersCollection;
        public UserRepository(IMongoDatabase database)
        {
            usersCollection = database.GetCollection<User>("Users");
        }
        public async Task<User> Get(string id)
        {
            var filter = Builders<User>.Filter.Eq("_id", new ObjectId(id));
            var user = await usersCollection.Find(filter).FirstOrDefaultAsync();
            return user;
        }

        public async Task Store(User user)
        {
            await usersCollection.InsertOneAsync(user);
        }

        public async Task Update(User user)
        {
            var filter = Builders<User>.Filter.Eq("_id", new ObjectId(user.Id));
            var update = Builders<User>.Update.Combine(
                Builders<User>.Update.Set(u => u.Description, user.Description)
            );

            await usersCollection.UpdateOneAsync(filter, update);
        }
    }
}
