using DTO.User;

namespace DTO.Chat;

public record ChatMemberResponse(
    UserPreviewResponse User,
    long? LastReadMessageId)
{
}

public record ChatFullResponse(
    long ChatId,
    long? ReceiverId,
    string DisplayName,
    string? AvatarUrl,
    string Type,
    DateTime CreatedAt,
    long LastMessageId,
    int UnreadMessageCount,
    List<ChatMemberResponse> ChatMembers)
{
}
