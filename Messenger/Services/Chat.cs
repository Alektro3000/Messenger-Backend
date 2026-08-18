using System.Linq.Expressions;
using DTO.Auth;
using DTO.Chat;
using DTO.User;
using Microsoft.EntityFrameworkCore;
using Models;
using Npgsql;
using Messenger.Realtime;
using Repository;
using SkiaSharp;

namespace Messenger.Services;

public sealed class ChatService(
    MessengerDbContext MessengerDbContext,
    ILogger<ChatService> Logger,
    IWebHostEnvironment Environment,
    RealtimeNotifier Notifier)
{
    public Task<List<ChatPreview>> QueryChats(long UserId, int skip = 0, int take = 20, String? query = null)
    {
        var useRequest = String.IsNullOrWhiteSpace(query);
        return MessengerDbContext.ChatMembers
            .AsNoTracking()
            .Where(x => x.UserId == UserId && (!useRequest || EF.Functions.ILike(
                x.Chat!.Type == ChatType.Direct ?
                    (x.Chat.DirectUser1Id == UserId ? x.Chat.DirectUser2!.DisplayName : x.Chat.DirectUser1!.DisplayName) :
                     x.Chat!.DisplayName!,
                      $"%{query}%")))
            .OrderByDescending(x => x.Chat!.LastMessage == null
                ? x.Chat.CreatedAt
                : x.Chat.LastMessage.SendTime)
            .Skip(skip)
            .Take(take)
            .Select(x => new ChatPreview(
                x.ChatId,

                //ReceiverId
                x.Chat!.Type == ChatType.Direct ?
                    (x.Chat.DirectUser1Id == UserId ? x.Chat.DirectUser2Id : x.Chat.DirectUser1Id) :
                     null,

                //Avatar Url
                x.Chat!.Type == ChatType.Direct ?
                    (x.Chat.DirectUser1Id == UserId ? x.Chat.DirectUser2!.AvatarUrl : x.Chat.DirectUser1!.AvatarUrl) :
                     x.Chat!.AvatarUrl,

                //DisplayName
                x.Chat!.Type == ChatType.Direct ?
                    (x.Chat.DirectUser1Id == UserId ? x.Chat.DirectUser2!.DisplayName : x.Chat.DirectUser1!.DisplayName) :
                     x.Chat!.DisplayName!,

                //Type
                x.Chat!.Type.ToString(),

                //Created At
                x.Chat!.CreatedAt,

                //Unread Count
                MessengerDbContext.Messages.Where(message =>
                    message.ChatId == x.ChatId &&
                    (x.LastReadMessageId == null || message.Id > x.LastReadMessageId))
                    .Count(),


                //LastMessage
                x.Chat.LastMessage == null ? null :
                new MessagePreviewResponse(
                    new MessagePreview(
                        x.Chat.LastMessage.Id!,
                        x.Chat.LastMessage.Text,
                        x.Chat.LastMessage.Type.ToString(),
                        x.Chat.LastMessage.SendTime,
                        x.Chat.LastMessage.EditTime,
                        x.Chat.LastMessage.DeleteTime),
                    new MessageUserPreview(
                        x.Chat.LastMessage.UserId,
                        x.Chat.LastMessage.User!.DisplayName,
                        x.Chat.LastMessage.User.AvatarUrl
                    )
                )
            ))
            .ToListAsync();
    }

    public async Task<ChatPreview?> QueryChatPreview(long userId, long chatId)
    {
        return await MessengerDbContext.ChatMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ChatId == chatId)
            .Select(x => new ChatPreview(
                x.ChatId,
                x.Chat!.Type == ChatType.Direct
                    ? (x.Chat.DirectUser1Id == userId ? x.Chat.DirectUser2Id : x.Chat.DirectUser1Id)
                    : null,
                x.Chat!.Type == ChatType.Direct
                    ? (x.Chat.DirectUser1Id == userId ? x.Chat.DirectUser2!.AvatarUrl : x.Chat.DirectUser1!.AvatarUrl)
                    : x.Chat!.AvatarUrl,
                x.Chat!.Type == ChatType.Direct
                    ? (x.Chat.DirectUser1Id == userId ? x.Chat.DirectUser2!.DisplayName : x.Chat.DirectUser1!.DisplayName)
                    : x.Chat!.DisplayName!,
                x.Chat!.Type.ToString(),
                x.Chat!.CreatedAt,
                MessengerDbContext.Messages.Count(message =>
                    message.ChatId == x.ChatId &&
                    (x.LastReadMessageId == null || message.Id > x.LastReadMessageId)),
                x.Chat.LastMessage == null ? null :
                new MessagePreviewResponse(
                    new MessagePreview(
                        x.Chat.LastMessage.Id!,
                        x.Chat.LastMessage.Text,
                        x.Chat.LastMessage.Type.ToString(),
                        x.Chat.LastMessage.SendTime,
                        x.Chat.LastMessage.EditTime,
                        x.Chat.LastMessage.DeleteTime),
                    new MessageUserPreview(
                        x.Chat.LastMessage.UserId,
                        x.Chat.LastMessage.User!.DisplayName,
                        x.Chat.LastMessage.User.AvatarUrl
                    ))
            ))
            .SingleOrDefaultAsync();
    }

    public async Task<ChatFullResponse?> QueryChatInfo(long userId, long chatId)
    {
        var chat = await MessengerDbContext.Chats
            .AsNoTracking()
            .Where(x => x.Id == chatId && x.ChatMembers.Any(member => member.UserId == userId))
            .Include(x => x.DirectUser1)
            .Include(x => x.DirectUser2)
            .SingleOrDefaultAsync();

        if (chat == null)
            return null;

        var currentMemberLastReadMessageId = await MessengerDbContext.ChatMembers
            .AsNoTracking()
            .Where(x => x.ChatId == chatId && x.UserId == userId)
            .Select(x => x.LastReadMessageId)
            .SingleAsync();

        var chatMembers = await MessengerDbContext.ChatMembers
            .AsNoTracking()
            .Where(x => x.ChatId == chatId)
            .OrderBy(x => x.User!.DisplayName)
            .Select(x => new ChatMemberResponse(
                new UserPreviewResponse(
                    x.User!.Id,
                    x.User.DisplayName,
                    x.User.AvatarUrl,
                    x.User.LastSeenAt),
                x.LastReadMessageId))
            .ToListAsync();

        var displayName = chat.Type == ChatType.Direct
            ? (chat.DirectUser1Id == userId ? chat.DirectUser2!.DisplayName : chat.DirectUser1!.DisplayName)
            : chat.DisplayName!;

        var avatarUrl = chat.Type == ChatType.Direct
            ? (chat.DirectUser1Id == userId ? chat.DirectUser2!.AvatarUrl : chat.DirectUser1!.AvatarUrl)
            : chat.AvatarUrl;

        var receiverId = chat.Type == ChatType.Direct
            ? (chat.DirectUser1Id == userId ? chat.DirectUser2Id : chat.DirectUser1Id)
            : null;

        var unreadCount = await MessengerDbContext.Messages
            .AsNoTracking()
            .CountAsync(message =>
                message.ChatId == chatId &&
                (currentMemberLastReadMessageId == null || message.Id > currentMemberLastReadMessageId));

        return new ChatFullResponse(
            chat.Id,
            receiverId,
            displayName,
            avatarUrl,
            chat.Type.ToString(),
            chat.CreatedAt,
            chat.LastMessageId ?? 0,
            unreadCount,
            chatMembers
        );
    }

    public async Task<ChatFullResponse?> UpdateChatInfo(long userId, long chatId, string displayName)
    {
        var chat = await MessengerDbContext.Chats
            .Include(x => x.ChatMembers)
            .ThenInclude(x => x.User)
            .Where(x => x.Id == chatId && x.ChatMembers.Any(member => member.UserId == userId))
            .SingleOrDefaultAsync();

        if (chat == null || chat.Type != ChatType.Group)
            return null;

        chat.DisplayName = String.IsNullOrWhiteSpace(displayName) ? chat.DisplayName : displayName.Trim();
        await MessengerDbContext.SaveChangesAsync();
        return await QueryChatInfo(userId, chatId);
    }

    public async Task<ChatFullResponse?> UploadAvatar(long userId, long chatId, IFormFile avatar)
    {
        if (avatar == null || avatar.Length == 0)
            return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Unsupported file type");

        var chat = await MessengerDbContext.Chats
            .Where(x => x.Id == chatId && x.ChatMembers.Any(member => member.UserId == userId))
            .SingleOrDefaultAsync();

        if (chat == null || chat.Type != ChatType.Group)
            return null;

        var oldUrl = chat.AvatarUrl;
        var folder = Path.Combine(Environment.WebRootPath, "uploads", "chat-avatars");
        var fileName = $"chat-{chatId}-{Guid.NewGuid()}{extension}";
        var newPath = Path.Combine(folder, fileName);

        Directory.CreateDirectory(folder);

        try
        {
            await using var validateStream = avatar.OpenReadStream();
            using var codec = SKCodec.Create(validateStream) ?? throw new ArgumentException("Invalid image");

            if (codec.Info.Width != codec.Info.Height)
                throw new ArgumentException("Avatar must be square");

            if (codec.Info.Width > 1024 || codec.Info.Height > 1024)
                throw new ArgumentException("Image too large");

            await using var inputStream = avatar.OpenReadStream();
            await using var outputStream = File.Create(newPath);
            await inputStream.CopyToAsync(outputStream);

            chat.AvatarUrl = $"/uploads/chat-avatars/{fileName}";
            await MessengerDbContext.SaveChangesAsync();

            if (oldUrl != null)
            {
                var oldPathSuffix = oldUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var oldPath = Path.Combine(Environment.WebRootPath, oldPathSuffix);
                try
                {
                    if (File.Exists(oldPath))
                        File.Delete(oldPath);
                }
                catch
                {
                    Logger.LogWarning("Failed to delete old chat avatar {oldPath}", oldPath);
                }
            }

            return await QueryChatInfo(userId, chatId);
        }
        catch
        {
            if (File.Exists(newPath))
                File.Delete(newPath);

            chat.AvatarUrl = oldUrl;
            throw;
        }
    }

    public async Task<ChatFullResponse?> CreateGroupChat(long userId, string displayName, List<long> memberIds)
    {
        var normalizedName = String.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        var normalizedMembers = memberIds
            .Where(id => id != userId)
            .Distinct()
            .ToList();

        if (normalizedName == null || normalizedMembers.Count == 0)
            return null;

        var knownUsers = await MessengerDbContext.Users
            .Where(x => normalizedMembers.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        if (knownUsers.Count != normalizedMembers.Count)
            return null;

        var chat = new Models.Chat
        {
            Type = ChatType.Group,
            DisplayName = normalizedName,
            AvatarUrl = null,
            CreatedAt = DateTime.UtcNow,
            ChatMembers =
            [
                new ChatMember { UserId = userId },
                ..normalizedMembers.Select(id => new ChatMember { UserId = id })
            ]
        };

        MessengerDbContext.Chats.Add(chat);
        await MessengerDbContext.SaveChangesAsync();

        var welcomeMessage = new Message
        {
            UserId = userId,
            ChatId = chat.Id,
            Type = MessageType.Text,
            Text = "Group chat created",
            SendTime = DateTime.UtcNow
        };

        MessengerDbContext.Messages.Add(welcomeMessage);
        await MessengerDbContext.SaveChangesAsync();

        chat.LastMessageId = welcomeMessage.Id;
        await MessengerDbContext.SaveChangesAsync();

        await MessengerDbContext.Entry(welcomeMessage)
            .Reference(m => m.User)
            .LoadAsync();

        var preview = new ChatPreview(
            chat.Id,
            null,
            chat.AvatarUrl,
            chat.DisplayName!,
            chat.Type.ToString(),
            chat.CreatedAt,
            0,
            new MessagePreviewResponse(
                new MessagePreview(
                    welcomeMessage.Id,
                    welcomeMessage.Text,
                    welcomeMessage.Type.ToString(),
                    welcomeMessage.SendTime,
                    welcomeMessage.EditTime,
                    welcomeMessage.DeleteTime
                ),
                new MessageUserPreview(
                    welcomeMessage.UserId,
                    welcomeMessage.User!.DisplayName,
                    welcomeMessage.User.AvatarUrl
                )
            )
        );

        await Notifier.NotifyUsersAsync(
            chat.ChatMembers.Select(x => x.UserId).Append(userId).Distinct(),
            new
            {
                type = "new_chat",
                chat = preview
            });

        return await QueryChatInfo(userId, chat.Id);
    }

    public async Task<DirectChatCreationResult> GetOrCreateDirectChat(long UserId, long OtherUserId)
    {
        var user1 = Math.Min(UserId, OtherUserId);
        var user2 = Math.Max(UserId, OtherUserId);

        var oldChat = await MessengerDbContext.Chats
            .AsNoTracking()
            .Where(x => x.Type == ChatType.Direct &&
                    x.DirectUser1Id == user1 &&
                    x.DirectUser2Id == user2)
            .FirstOrDefaultAsync();

        if (oldChat != null)
            return new DirectChatCreationResult(oldChat.Id, false);

        var chat = new Models.Chat
        {
            Type = ChatType.Direct,
            DirectUser1Id = user1,
            DirectUser2Id = user2,
            CreatedAt = DateTime.UtcNow,
            ChatMembers =
            [
                new ChatMember { UserId = UserId },
                new ChatMember { UserId = OtherUserId }
            ]
        };
        MessengerDbContext.Chats.Add(chat);
        try
        {
            await MessengerDbContext.SaveChangesAsync();
            return new DirectChatCreationResult(chat.Id, true);
        }
        catch (DbUpdateException _)
        {
            return new DirectChatCreationResult(
                await MessengerDbContext.Chats
                    .AsNoTracking()
                    .Where(c =>
                        c.Type == Models.ChatType.Direct &&
                        c.DirectUser1Id == user1 &&
                        c.DirectUser2Id == user2)
                    .Select(c => c.Id)
                    .FirstAsync(),
                false);
        }
    }

}


public record struct DirectChatCreationResult(
    long ChatId,
    bool Created
);
