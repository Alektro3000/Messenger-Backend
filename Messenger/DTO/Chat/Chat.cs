namespace DTO.Chat;

public record Chat(
    long Id,
    string? AvatarUri,
    string DisplayName,
    DateTime CreatedAt)
{
}

public record ChatUpdateRequest(
    long ChatId,
    string DisplayName)
{
}

public record ChatCreateRequest(
    string DisplayName,
    List<long> MemberIds)
{
}
