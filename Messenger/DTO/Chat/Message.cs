namespace DTO.Chat;
public record MessageUserPreview(
    long Id,
    String DisplayName,
    String? AvatarUrl
){}

public record MessagePreview(
    long Id,
    string? Text,
    string Type,
    DateTime SendAt,
    DateTime? EditAt,
    DateTime? DeleteAt
){}

public record MessagePreviewResponse(
    MessagePreview Message,
    MessageUserPreview Sender)
{
}

public record ChatPreview(
    long ChatId,
    long? ReceiverId,
    string? AvatarUrl,
    string DisplayName,
    string Type,
    DateTime CreatedAt,
    int UnreadMessageCount,
    MessagePreviewResponse? LastMessage)
{
}
public record MessageFull(
    long UserId,
    long ChatId,
    string Type,
    string? Text,
    DateTime SendTime)
{
}

public record SendMessageRequest(
    string Type,
    string? Text
){}


public record SendMessageResponse(
    long ChatId,
    long MessageId,
    DateTime SendAt
){}
