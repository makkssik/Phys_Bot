using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using WeatherBot.Entities;
using WeatherBot.Entities.ValueObjects;
using WeatherBot.Interfaces.Repositories;

namespace WeatherBot.Repositories;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteUserRepository> _logger;

    public SqliteUserRepository(ILogger<SqliteUserRepository> logger, string databasePath = "users.db")
    {
        _logger = logger;
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        };

        _connectionString = builder.ToString();
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            await EnableForeignKeysAsync(connection);

            var command = connection.CreateCommand();
            command.CommandText =
                """
                PRAGMA foreign_keys = ON;
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY,
                    Username TEXT NOT NULL,
                    IsMotorist INTEGER NOT NULL DEFAULT 0,
                    Age INTEGER NULL,
                    Gender TEXT NOT NULL DEFAULT 'unknown',
                    Hobbies TEXT NOT NULL DEFAULT ''
                );
                CREATE TABLE IF NOT EXISTS Subscriptions (
                    Id TEXT PRIMARY KEY,
                    UserId INTEGER NOT NULL,
                    LocationName TEXT NOT NULL,
                    Latitude REAL NOT NULL,
                    Longitude REAL NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    SendDailyWeather INTEGER NOT NULL,
                    SendEmergencyAlerts INTEGER NOT NULL,
                    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );
                """;

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Database initialization failed!");
            throw;
        }
    }

    public async Task<User?> FindUserAsync(long userId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnableForeignKeysAsync(connection);

        return await ReadUserAsync(connection, userId);
    }

    public async Task<User> GetUserAsync(long userId)
    {
        var user = await FindUserAsync(userId);
        if (user != null)
            return user;

        var newUser = new User(userId, $"user_{userId}");
        await AddUserAsync(newUser);
        return newUser;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnableForeignKeysAsync(connection);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT u.Id, u.Username, u.IsMotorist, u.Age, u.Gender, u.Hobbies,
                   s.Id, s.LocationName, s.Latitude, s.Longitude, s.CreatedAt, s.SendDailyWeather, s.SendEmergencyAlerts
            FROM Users u
            LEFT JOIN Subscriptions s ON s.UserId = u.Id
            ORDER BY u.Id;
            """;

        var users = new Dictionary<long, User>();

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var userId = reader.GetInt64(0);

            if (!users.TryGetValue(userId, out var user))
            {
                var username = reader.GetString(1);
                var isMotorist = reader.GetInt32(2) == 1;
                int? age = reader.IsDBNull(3) ? null : reader.GetInt32(3);
                var gender = reader.GetString(4);
                var hobbies = reader.GetString(5);

                user = new User(userId, username);
                user.UpdateProfile(age, gender, hobbies, isMotorist);
                users[userId] = user;
            }

            if (reader.IsDBNull(6))
                continue;

            var subscription = new Subscription(
                Guid.Parse(reader.GetString(6)),
                userId,
                reader.GetString(7),
                new Coordinate(reader.GetDouble(8), reader.GetDouble(9)),
                reader.GetInt32(11) == 1,
                reader.GetInt32(12) == 1,
                DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            user.Subscriptions.Add(subscription);
        }

        return users.Values.ToList();
    }

    public async Task AddUserAsync(User user)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnableForeignKeysAsync(connection);
        await SaveUserAsync(connection, user);
    }

    public async Task UpdateUserAsync(User user)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnableForeignKeysAsync(connection);
        await SaveUserAsync(connection, user);
    }

    private async Task SaveUserAsync(SqliteConnection connection, User user)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        var sqliteTransaction = (SqliteTransaction)transaction;

        var upsertUser = connection.CreateCommand();
        upsertUser.Transaction = sqliteTransaction;
        upsertUser.CommandText =
            """
            INSERT INTO Users (Id, Username, IsMotorist, Age, Gender, Hobbies)
            VALUES ($id, $username, $isMotorist, $age, $gender, $hobbies)
            ON CONFLICT(Id) DO UPDATE SET 
                Username = excluded.Username,
                IsMotorist = excluded.IsMotorist,
                Age = excluded.Age,
                Gender = excluded.Gender,
                Hobbies = excluded.Hobbies;
            """;
        upsertUser.Parameters.AddWithValue("$id", user.Id);
        upsertUser.Parameters.AddWithValue("$username", user.Username);
        upsertUser.Parameters.AddWithValue("$isMotorist", user.IsMotorist ? 1 : 0);
        upsertUser.Parameters.AddWithValue("$age", user.Age.HasValue ? user.Age.Value : DBNull.Value);
        upsertUser.Parameters.AddWithValue("$gender", user.Gender);
        upsertUser.Parameters.AddWithValue("$hobbies", user.Hobbies);

        await upsertUser.ExecuteNonQueryAsync();

        var deleteSubscriptions = connection.CreateCommand();
        deleteSubscriptions.Transaction = sqliteTransaction;
        deleteSubscriptions.CommandText = "DELETE FROM Subscriptions WHERE UserId = $userId;";
        deleteSubscriptions.Parameters.AddWithValue("$userId", user.Id);
        await deleteSubscriptions.ExecuteNonQueryAsync();

        foreach (var subscription in user.Subscriptions)
        {
            var insertSubscription = connection.CreateCommand();
            insertSubscription.Transaction = sqliteTransaction;
            insertSubscription.CommandText =
                """
                INSERT INTO Subscriptions (Id, UserId, LocationName, Latitude, Longitude, CreatedAt, SendDailyWeather, SendEmergencyAlerts)
                VALUES ($id, $userId, $locationName, $latitude, $longitude, $createdAt, $sendDaily, $sendEmergency);
                """;

            insertSubscription.Parameters.AddWithValue("$id", subscription.Id.ToString());
            insertSubscription.Parameters.AddWithValue("$userId", user.Id);
            insertSubscription.Parameters.AddWithValue("$locationName", subscription.LocationName);
            insertSubscription.Parameters.AddWithValue("$latitude", subscription.Coordinate.Latitude);
            insertSubscription.Parameters.AddWithValue("$longitude", subscription.Coordinate.Longitude);
            insertSubscription.Parameters.AddWithValue("$createdAt",
                subscription.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            insertSubscription.Parameters.AddWithValue("$sendDaily", subscription.SendDailyWeather ? 1 : 0);
            insertSubscription.Parameters.AddWithValue("$sendEmergency", subscription.SendEmergencyAlerts ? 1 : 0);

            await insertSubscription.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<User?> ReadUserAsync(SqliteConnection connection, long userId)
    {
        var userCommand = connection.CreateCommand();
        userCommand.CommandText = "SELECT Id, Username, IsMotorist, Age, Gender, Hobbies FROM Users WHERE Id = $id;";
        userCommand.Parameters.AddWithValue("$id", userId);

        await using var reader = await userCommand.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        var user = new User(reader.GetInt64(0), reader.GetString(1));
        user.UpdateProfile(
            reader.IsDBNull(3) ? null : reader.GetInt32(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetInt32(2) == 1
        );
        await reader.DisposeAsync();

        var subscriptionsCommand = connection.CreateCommand();
        subscriptionsCommand.CommandText =
            """
            SELECT Id, LocationName, Latitude, Longitude, CreatedAt, SendDailyWeather, SendEmergencyAlerts
            FROM Subscriptions
            WHERE UserId = $userId;
            """;
        subscriptionsCommand.Parameters.AddWithValue("$userId", userId);

        await using var subsReader = await subscriptionsCommand.ExecuteReaderAsync();
        while (await subsReader.ReadAsync())
        {
            var subscriptionId = Guid.Parse(subsReader.GetString(0));
            var locationName = subsReader.GetString(1);
            var latitude = subsReader.GetDouble(2);
            var longitude = subsReader.GetDouble(3);
            var createdAt = DateTime.Parse(subsReader.GetString(4), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            var sendDailyWeather = subsReader.GetInt32(5) == 1;
            var sendEmergencyAlerts = subsReader.GetInt32(6) == 1;

            var subscription = new Subscription(
                subscriptionId,
                userId,
                locationName,
                new Coordinate(latitude, longitude),
                sendDailyWeather,
                sendEmergencyAlerts,
                createdAt);

            user.Subscriptions.Add(subscription);
        }

        return user;
    }

    private static async Task EnableForeignKeysAsync(SqliteConnection connection)
    {
        var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        await pragma.ExecuteNonQueryAsync();
    }
}