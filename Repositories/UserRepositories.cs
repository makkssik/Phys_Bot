using WeatherBot.Interfaces.Repositories;
using WeatherBot.Entities;
using System.Text.Json;
using System.IO;

namespace WeatherBot.Repositories;

public class UserRepository : IUserRepository
{
    private List<User> _users = new();
    private const string StorageFile = "users.json";

    public UserRepository()
    {
        // Optionally, load users immediately on construction (for DI, you may want to control timing)
        LoadFromFileAsync(StorageFile).Wait();
    }

    public async Task<User?> FindUserAsync(long userId)
    {
        return _users.FirstOrDefault(u => u.Id == userId);
    }

    public async Task<User> GetUserAsync(long userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            user = new User(userId, $"user_{userId}");
            _users.Add(user);
            await SaveToFileAsync(StorageFile); // Save after change
        }
        return user;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return new List<User>(_users);
    }

    public async Task AddUserAsync(User user)
    {
        if (!_users.Any(u => u.Id == user.Id))
        {
            _users.Add(user);
            await SaveToFileAsync(StorageFile); // Save after change
        }
    }

    public async Task UpdateUserAsync(User user)
    {
        var existing = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            _users.Remove(existing);
        }
        _users.Add(user);
        await SaveToFileAsync(StorageFile); // Save after change
    }

    // --- Persistence Methods ---
    public async Task SaveToFileAsync(string filename)
    {
        var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filename, json);
    }

    public async Task LoadFromFileAsync(string filename)
    {
        if (File.Exists(filename))
        {
            var json = await File.ReadAllTextAsync(filename);
            var loaded = JsonSerializer.Deserialize<List<User>>(json);
            if (loaded != null)
                _users = loaded;
        }
    }
}