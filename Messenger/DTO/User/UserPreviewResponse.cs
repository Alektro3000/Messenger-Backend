namespace DTO.User;

public record UserPreviewResponse(
    long Id,
    string DisplayName,
    string? AvatarUrl,
    DateTime LastSeenAt)
{
}
