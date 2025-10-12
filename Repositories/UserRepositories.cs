using WeatherBot.Interfaces.Repositories;
using WeatherBot.Entities;

namespace WeatherBot.Repositories;

public class UserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> FindUserAsync(long userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        return Task.FromResult(user);
    }

    public Task<User> GetUserAsync(long userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            user = new User(userId, $"user_{userId}");
            _users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(new List<User>(_users));
    }

    public Task AddUserAsync(User user)
    {
        if (!_users.Any(u => u.Id == user.Id))
        {
            _users.Add(user);
        }
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(User user)
    {
        var existing = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            _users.Remove(existing);
        }
        _users.Add(user);
        return Task.CompletedTask;
    }
}