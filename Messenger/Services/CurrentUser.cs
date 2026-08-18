

namespace Messenger.Services;
public class CurrentUser
{
    public long UserId { get; }
    public long SessionId { get; }

    public CurrentUser(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

        if (!long.TryParse(user?.FindFirst("sid")?.Value, out var sessionId))
            throw new UnauthorizedAccessException("Invalid session ID");

        if (!long.TryParse(user?.FindFirst("uid")?.Value, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID");

        UserId = userId;
        SessionId = sessionId;
    }
}