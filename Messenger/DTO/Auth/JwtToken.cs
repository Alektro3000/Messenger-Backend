namespace DTO.Auth;
public record JwtToken(string AccessToken, string RefreshToken, string TokenType, int ExpiresInMinutes)
{
}
