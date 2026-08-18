using DTO.User;
using Messenger.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Repository;

namespace Messenger.Tests;

public class UserServiceTests
{
    private static MessengerDbContext CreateDb()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MessengerDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new MessengerDbContext(options);
        db.Database.EnsureCreated();

        return db;
    }

    private static UserService CreateUserService(MessengerDbContext db)
    {
        return new UserService(
            db,
            NullLogger<UserService>.Instance,
            new TestWebHostEnvironment());
    }

    [Fact]
    public async Task QueryUsers_FindsMatchingUsers()
    {
        await using var db = CreateDb();
        var service = CreateUserService(db);

        db.Users.AddRange(
            CreateUser("bob", "Bob Builder", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateUser("alice", "Alice Wonderland", new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateUser("charlie", "Charles Brown", new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync();

        var result = await service.QueryUsers("lic", 0, 20);

        Assert.Single(result);
        Assert.Equal("Alice Wonderland", result[0].DisplayName);
    }

    [Fact]
    public async Task QueryUser_ReturnsUser()
    {
        await using var db = CreateDb();
        var service = CreateUserService(db);

        var createdAt = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var lastSeenAt = new DateTime(2024, 2, 4, 4, 5, 6, DateTimeKind.Utc);
        var user = new Models.User
        {
            Username = "bob",
            DisplayName = "Bob Builder",
            Name = "Bob",
            Surname = "Builder",
            Bio = "hello",
            AvatarUrl = "avatar.png",
            CreatedAt = createdAt,
            LastSeenAt = lastSeenAt
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await service.QueryUser(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.DisplayName, result.DisplayName);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal(user.Surname, result.Surname);
        Assert.Equal(user.Bio, result.Bio);
        Assert.Equal(user.AvatarUrl, result.AvatarUrl);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(lastSeenAt, result.LastSeenAt);
    }

    [Fact]
    public async Task QueryUser_UserNotFound_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateUserService(db);

        var result = await service.QueryUser(123);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUser_UpdatesProfileAndDisplayName()
    {
        await using var db = CreateDb();
        var service = CreateUserService(db);

        var user = CreateUser("bob", "Bob", DateTime.UtcNow);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var result = await service.UpdateUser(
            user.Id,
            new UserUpdate("Robert", "Builderman", "updated bio"));

        Assert.Equal(ChangeProfileResult.Succesful, result);

        var updatedUser = await db.Users.SingleAsync(x => x.Id == user.Id);
        Assert.Equal("bob", updatedUser.Username);
        Assert.Equal("Robert", updatedUser.Name);
        Assert.Equal("Builderman", updatedUser.Surname);
        Assert.Equal("updated bio", updatedUser.Bio);
        Assert.Equal("Builderman Robert", updatedUser.DisplayName);
    }

    [Fact]
    public async Task UpdateUser_UserNotFound_ReturnsUserNotFound()
    {
        await using var db = CreateDb();
        var service = CreateUserService(db);

        var result = await service.UpdateUser(
            123,
            new UserUpdate("Ghost", null, null));

        Assert.Equal(ChangeProfileResult.UserNotFound, result);
    }

    private static Models.User CreateUser(
        string username,
        string displayName,
        DateTime createdAt)
    {
        return new Models.User
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            CreatedAt = createdAt,
            LastSeenAt = createdAt
        };
    }
}
