using System.Text.Json;
using Movement_Home_Task.Models;

namespace Movement_Home_Task.Persistence
{
    public class FileStorage : IDataStorage
    {
        private readonly string filePath;
        
        public FileStorage()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            filePath = Path.Combine(baseDir, "UsersStorage.json");

            var dir = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(filePath)) File.WriteAllText(filePath, "[]");
        }

        public async Task<User?> GetUserById(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "Identifier is required in 'FileStorage -> GetUserByIdentifier'");
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var users = JsonSerializer.Deserialize<List<KeyValuePair<User, DateTime>>>(json) ?? null;

                if (users == null || users.Count == 0)
                {
                    return null;
                }

                var user = users.FirstOrDefault(u =>
                    u.Key != null && u.Key.Id != null && !string.IsNullOrEmpty(u.Key.Id) &&
                    u.Key.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                    DateTime.UtcNow - u.Value < TimeSpan.FromMinutes(30));

                if (user.Key == null)
                {
                    return null;
                }

                return user.Key;
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in 'FileStorage -> GetUserById': {ex.Message}");
            }
        }

        public async Task StoreUser(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentNullException(nameof(user), "Valid User object with ID is required");
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var users = JsonSerializer.Deserialize<List<KeyValuePair<User, DateTime>>>(json) ?? new();

                users.RemoveAll(
                    u =>
                        u.Key != null && !string.IsNullOrEmpty(u.Key.Id) &&
                        u.Key.Id.Equals(user.Id, StringComparison.OrdinalIgnoreCase));

                users.Add(new KeyValuePair<User, DateTime>(user, DateTime.UtcNow));

                var updatedJson = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception in 'FileStorage -> GetUserByIdentifier': {ex.Message}");
            }
        }
    }
}
