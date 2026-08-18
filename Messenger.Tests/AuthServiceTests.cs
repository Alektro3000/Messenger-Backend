using DTO.Auth;
using Messenger.Security;
using Messenger.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Repository;

namespace Messenger.Tests;

public class AuthServiceTests
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

    private static AuthService CreateAuthService(MessengerDbContext db)
    {
        var testOptions = new JwtOptions
        {
            Issuer = "Messenger.Tests",
            Audience = "Messenger.Tests",
            SecretKey = "TestKeyForMessengerJwtTokens1234567890",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        return new AuthService(
            Options.Create(testOptions),
            db,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterUser_CreatesUser()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);

        var result = await service.RegisterUser("bob", "123456");

        Assert.Equal(RegisterResult.Succesful, result);

        var user = await db.Users.SingleAsync(x => x.Username == "bob");

        Assert.Equal("bob", user.DisplayName);
        Assert.True(BCrypt.Net.BCrypt.Verify("123456", user.PasswordHash));
    }

    [Fact]
    public async Task ValidateUser_UserNotFound_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);

        var result = await service.ValidateUser("bob", "123456");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUser_IncorrectPassword_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);
        db.Users.Add(new Models.User
        {
            Username = "bob",
            DisplayName = "Bob",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.ValidateUser("bob", "wrong");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateUser_CorrectPassword_ReturnsUser()
    {
        await using var db = CreateDb();
        var service = CreateAuthService(db);
        db.Users.Add(new Models.User
        {
            Username = "bob",
            DisplayName = "Bob",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.ValidateUser("bob", "123456");

        Assert.NotNull(result);
        Assert.Equal("bob", result!.Username);
    }
}
