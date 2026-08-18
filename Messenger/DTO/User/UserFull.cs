namespace DTO.User;

public record UserFull(
    long Id,
    string Username,
    string DisplayName,
    string? Name,
    string? Surname,
    string? Bio,
    string? AvatarUrl,
    DateTime CreatedAt,
    DateTime LastSeenAt)
{
}

public record UserUpdate(
    string? Name,
    string? Surname,
    string? Bio)
{
}

public record UserPreview(
    long Id,
    String DisplayName,
    String? AvatarUrl,
    DateTime LastSeenAt
){}
