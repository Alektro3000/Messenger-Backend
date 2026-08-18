using Messenger.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Models;
using Repository;

namespace Messenger.Tests;

public class ChatServiceTests
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

    private static ChatService CreateChatService(MessengerDbContext db)
    {
        return new ChatService(
            db,
            NullLogger<ChatService>.Instance,
            new TestWebHostEnvironment());
    }

    [Fact]
    public async Task QueryChats_ReturnsChatsOrderedByLatestActivity()
    {
        await using var db = CreateDb();
        var service = CreateChatService(db);

        var user = CreateUser(1, "bob", "Bob");
        var oldChat = CreateGroupChat("Old", new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var newChat = CreateGroupChat("New", new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        db.Users.Add(user);
        db.Chats.AddRange(oldChat, newChat);
        await db.SaveChangesAsync();

        db.ChatMembers.AddRange(
            new ChatMember { UserId = user.Id, ChatId = oldChat.Id },
            new ChatMember { UserId = user.Id, ChatId = newChat.Id });
        await db.SaveChangesAsync();

        var result = await service.QueryChats(user.Id, query: "skip-filter");

        Assert.Equal(2, result.Count);
        Assert.Equal(newChat.Id, result[0].ChatId);
        Assert.Equal("New", result[0].DisplayName);
        Assert.Equal(oldChat.Id, result[1].ChatId);
        Assert.Equal("Old", result[1].DisplayName);
    }

    [Fact]
    public async Task GetOrCreateDirectChat_CreatesChatAndMembers()
    {
        await using var db = CreateDb();
        var service = CreateChatService(db);

        db.Users.AddRange(
            CreateUser(1, "alice", "Alice"),
            CreateUser(2, "bob", "Bob"));
        await db.SaveChangesAsync();

        var chatId = await service.GetOrCreateDirectChat(2, 1);

        var chat = await db.Chats.SingleAsync();
        Assert.Equal(chat.Id, chatId);
        Assert.Equal(ChatType.Direct, chat.Type);
        Assert.Equal(1, chat.DirectUser1Id);
        Assert.Equal(2, chat.DirectUser2Id);

        var members = await db.ChatMembers
            .OrderBy(x => x.UserId)
            .ToListAsync();

        Assert.Equal(2, members.Count);
        Assert.Equal(1, members[0].UserId);
        Assert.Equal(chat.Id, members[0].ChatId);
        Assert.Equal(2, members[1].UserId);
        Assert.Equal(chat.Id, members[1].ChatId);
    }

    [Fact]
    public async Task GetOrCreateDirectChat_ReusesExistingChat()
    {
        await using var db = CreateDb();
        var service = CreateChatService(db);

        db.Users.AddRange(
            CreateUser(1, "alice", "Alice"),
            CreateUser(2, "bob", "Bob"));
        await db.SaveChangesAsync();

        var firstChatId = await service.GetOrCreateDirectChat(1, 2);
        var secondChatId = await service.GetOrCreateDirectChat(2, 1);

        Assert.Equal(firstChatId, secondChatId);
        Assert.Equal(1, await db.Chats.CountAsync());
    }

    [Fact]
    public async Task CreateGroupChat_CreatesChatWithRequesterAndMembers()
    {
        await using var db = CreateDb();
        var service = CreateChatService(db);

        db.Users.AddRange(
            CreateUser(1, "alice", "Alice"),
            CreateUser(2, "bob", "Bob"),
            CreateUser(3, "charlie", "Charlie"));
        await db.SaveChangesAsync();

        var result = await service.CreateGroupChat(1, "Project", [2, 3, 3]);

        Assert.NotNull(result);
        Assert.Equal("Project", result!.DisplayName);
        Assert.Equal("Group", result.Type);
        Assert.Equal(3, result.ChatMembers.Count);

        var members = await db.ChatMembers
            .OrderBy(x => x.UserId)
            .Select(x => x.UserId)
            .ToListAsync();

        Assert.Equal([1, 2, 3], members);
    }

    [Fact]
    public async Task CreateGroupChat_UnknownMember_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = CreateChatService(db);

        db.Users.Add(CreateUser(1, "alice", "Alice"));
        await db.SaveChangesAsync();

        var result = await service.CreateGroupChat(1, "Project", [404]);

        Assert.Null(result);
        Assert.Empty(db.Chats);
    }

    private static Models.User CreateUser(long id, string username, string displayName)
    {
        return new Models.User
        {
            Id = id,
            Username = username,
            DisplayName = displayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
    }

    private static Chat CreateGroupChat(string displayName, DateTime createdAt)
    {
        return new Chat
        {
            Type = ChatType.Group,
            DisplayName = displayName,
            AvatarUrl = $"{displayName}.png",
            CreatedAt = createdAt
        };
    }
}
