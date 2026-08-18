namespace Messenger.Security;
public record JwtOptions
{
    public required string SecretKey { get; init; }
    public string Issuer { get; init; } = "MessengerApp";
    public string Audience { get; init; } = "MessengerAppUsers";
    public int ExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 30;
}