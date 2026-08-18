using System.Linq.Expressions;
using DTO.Auth;
using DTO.Chat;
using Microsoft.EntityFrameworkCore;
using Models;
using Npgsql;
using Repository;
using Messenger.Realtime;
using System.Security.Authentication;
using System.ComponentModel.DataAnnotations;

namespace Messenger.Services;

public sealed class MessageService(
    MessengerDbContext MessengerDbContext,
    RealtimeNotifier Notifier,
    ILogger<ChatService> Logger)
{
    public static readonly Expression<Func<Models.Message, MessagePreviewResponse>> MessageToMessageEntity =
     x =>
    new MessagePreviewResponse(
                    new MessagePreview(
                        x.Id,
                        x.Text,
                        x.Type.ToString(),
                        x.SendTime,
                        x.EditTime,
                        x.DeleteTime
                    ),
                    new MessageUserPreview(
                        x.UserId,
                        x.User!.DisplayName,
                        x.User.AvatarUrl
                    ));
    static MessagePreviewResponse CreateResponse(Message x)
    {
        return new MessagePreviewResponse(
                    new MessagePreview(
                        x.Id,
                        x.Text,
                        x.Type.ToString(),
                        x.SendTime,
                        x.EditTime,
                        x.DeleteTime
                    ),
                    new MessageUserPreview(
                        x.UserId,
                        x.User!.DisplayName,
                        x.User.AvatarUrl
                    ));
    }
    

    public async Task<List<MessagePreviewResponse>> GetMessages(long userId, long ChatId, long? beforeMessageId = null, int take = 20)
    {
        var chatMember = MessengerDbContext.ChatMembers
            .Find(ChatId, userId);

        if(chatMember == null)
            return [];

        var messages = await MessengerDbContext.Messages
            .AsNoTracking()
            .Where(x => x.ChatId == ChatId && x.DeleteTime == null &&
                (beforeMessageId == null || x.Id < beforeMessageId))
            .OrderByDescending(x => x.Id)
            .Take(take)
            .Select(MessageToMessageEntity)
            .ToListAsync();
        var newestLoadedMessageId  = messages.FirstOrDefault()?.Message?.Id;

        if((newestLoadedMessageId ?? long.MinValue) > (chatMember.LastReadMessageId ?? long.MinValue))
            chatMember.LastReadMessageId = newestLoadedMessageId;
        await MessengerDbContext.SaveChangesAsync();

        return messages;
    }

    public async Task<SendMessageResponse> SendMessage(long ChatId, long SenderId, SendMessageRequest message)
    {
        var chatMembers = MessengerDbContext.ChatMembers
            .Where(x=> x.ChatId == ChatId).ToList();

        if(!chatMembers.Any(x=>x.UserId == SenderId))
            throw new AuthenticationException($"${SenderId} doesn't belong to group ${ChatId}");

        var newMessage = new Message()
        {
            UserId = SenderId,
            ChatId = ChatId,
            Type = Enum.Parse<MessageType>(message.Type),
            Text = message.Text,
            SendTime = DateTime.UtcNow
        };

        chatMembers.First(x=>x.UserId == SenderId).LastReadMessage = newMessage;
        MessengerDbContext.Messages.Add(newMessage);

        var chat = await MessengerDbContext.Chats.FindAsync(ChatId) ?? throw new ArgumentException("Chat not found");

        await MessengerDbContext.SaveChangesAsync();

        await MessengerDbContext.Chats
            .Where(c => c.Id == ChatId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.LastMessageId,
                c => c.LastMessageId == null || c.LastMessageId < newMessage.Id
                ? newMessage.Id
                : c.LastMessageId));

        await MessengerDbContext.Entry(newMessage)
            .Reference(m => m.User)
            .LoadAsync();

        await SendNotification(ChatId, SenderId, CreateResponse(newMessage));

        return new SendMessageResponse(ChatId, newMessage.Id, newMessage.SendTime);
    }

    public async Task<DeleteMessageResponse?> DeleteMessage(long ChatId, long MessageId, long userId)
    {
        var chatMember = MessengerDbContext.ChatMembers
            .Find(ChatId, userId);

        if(chatMember == null)
            return null;

        var message = MessengerDbContext.Messages
            .Find(MessageId);
        
        if(message == null)
            return null;
            
        if(message.ChatId != ChatId)
            return null;

        if (message.DeleteTime != null)
            return null;

        var chat = MessengerDbContext.Chats
            .Find(ChatId);
        
        if(chat == null)
        {
            Logger.LogWarning("Chat not found for existing chatMember {ChatMember} and message {message}", chatMember, message);
            return null;
        }

        //Update last message Id
        if(chat.LastMessageId == MessageId)
        {
            chat.LastMessageId = 
                MessengerDbContext.Messages
                    .Where(x=>x.ChatId == ChatId && x.DeleteTime == null && x.Id != MessageId)
                    .Max(x=>x.Id);
        }
            

        MessageHistory messageHistory = new MessageHistory()
        {
            Id = message.Id,
            Version = message.CurrentVersion,
            Text = message.Text,
            SendTime = message.EditTime ?? message.SendTime,
        };

        message.DeleteTime = DateTime.UtcNow;
        message.CurrentVersion++;
        message.Text = null;
        MessengerDbContext.MessageHistories.Add(messageHistory);
        await MessengerDbContext.SaveChangesAsync();

        var messageResponse = new DeleteMessageResponse(
            message.Id, 
            ChatId,
            chat.LastMessageId,
            (DateTime)message.DeleteTime!
            );
        
        await SendNotification(ChatId, userId, messageResponse, "delete_message");

        return messageResponse;
    }
    public async Task SendNotification<T>(long ChatId, long UserId, T data, string Type = "new_message")
    {
        
        var chatMembers = 
            MessengerDbContext.ChatMembers
            .Where(x=> x.ChatId == ChatId && x.UserId != UserId)
            .Select(x=>x.UserId).ToList();

        await Notifier.NotifyUsersAsync(chatMembers, 
            new {
                type = Type,
                chatId = ChatId, 
                message = data
                });
        
    }
    public async Task<EditMessageResponse?> EditTextMessage(long ChatId, long MessageId, long userId, string newText)
    {
        var chatMember = MessengerDbContext.ChatMembers
            .Find(ChatId, userId);

        if(chatMember == null)
            return null;

        var message = MessengerDbContext.Messages
            .Find(MessageId);
        
        if(message == null)
            return null;
            
        if(message.ChatId != ChatId)
            return null;

        if (message.DeleteTime != null)
            return null;
            

        MessageHistory messageHistory = new MessageHistory()
        {
            Id = message.Id,
            Version = message.CurrentVersion,
            Text = message.Text,
            SendTime = message.EditTime ?? message.SendTime,
        };

        message.CurrentVersion++;
        message.Text = newText;
        message.EditTime = DateTime.UtcNow;
        MessengerDbContext.MessageHistories.Add(messageHistory);
        await MessengerDbContext.SaveChangesAsync();
        var editMessageResponse = new EditMessageResponse(message.Id, message.Text, (DateTime)message.EditTime);

        await SendNotification(ChatId, userId, editMessageResponse, "edit_message");

        return editMessageResponse;
    }
}


public record struct DeleteMessageResponse(
    long MessageId,
    long ChatId,
    long? NewLastMessageId,
    DateTime DeleteAt
);


public record struct EditMessageResponse(
    long MessageId,
    String NewText,
    DateTime EditAt
);
