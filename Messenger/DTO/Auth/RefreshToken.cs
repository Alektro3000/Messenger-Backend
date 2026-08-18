namespace DTO.Auth;
public record RefreshToken(long SessionId, string Secret)
{
}
