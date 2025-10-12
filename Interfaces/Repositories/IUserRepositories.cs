using WeatherBot.Entities;

namespace WeatherBot.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> FindUserAsync(long userId);
    Task<User> GetUserAsync(long userId);
    Task<List<User>> GetAllUsersAsync();
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
}