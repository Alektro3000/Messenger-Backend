
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DTO.Auth;
using Messenger.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System.Security.Cryptography;
using Npgsql;
using Repository;

namespace Messenger.Services;
public sealed class AuthService(IOptions<JwtOptions> jwtOptions, 
        MessengerDbContext MessengerDbContext, ILogger<AuthService> Logger)
{

    private readonly JwtOptions JwtOptions = jwtOptions.Value;

    public async Task<User?> ValidateUser(string username, string password)
    {
        var user = await MessengerDbContext.Users
            .AsNoTracking()
            .Select(x => new { x.Id, x.Username, x.PasswordHash })
            .SingleOrDefaultAsync(x => x.Username == username);

        if (user == null)
            return null;
        
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return new User()
        {
            UserId = user.Id,
            Username = user.Username
        };
    }
    
    public async Task<RegisterResult> RegisterUser(string username, string password)
    {
        try
        {
            var user = new Models.User
            {
                Username = username,
                DisplayName = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };

            MessengerDbContext.Add(user);

            await MessengerDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pgEx &&
                pgEx.SqlState == PostgresErrorCodes.UniqueViolation &&
                pgEx.ConstraintName == "UniqueIndex_Users_UserName")
        {
            Logger.LogWarning(ex,
                "Registration failed because username {Username} already exists.", 
                username);
            
            return RegisterResult.RepeatedUser; 
        }

        return RegisterResult.Succesful; 
    }
    public RefreshToken? GetRefreshTokenFromJWT(string refreshTokenJwt)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(JwtOptions.SecretKey);

            var principal = tokenHandler.ValidateToken(
                refreshTokenJwt,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = JwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = JwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out _
            );

            var tokenType = principal.FindFirst("typ")?.Value;
            if (tokenType != "refresh")
                return null;

            var sessionIdValue = principal.FindFirst("sid")?.Value;
            var secret = principal.FindFirst("sec")?.Value;

            if (!int.TryParse(sessionIdValue, out var sessionId))
                return null;

            if (string.IsNullOrWhiteSpace(secret))
                return null;

            return new RefreshToken(sessionId, secret);
        }
        catch
        {
            return null;
        }
    }

    private string GenerateRefreshTokenJWT(RefreshToken refreshToken)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(JwtOptions.SecretKey));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("typ", "refresh"),
            new Claim("sid", refreshToken.SessionId.ToString()),
            new Claim("sec", refreshToken.Secret)
        };

        var token = new JwtSecurityToken(
            issuer: JwtOptions.Issuer,
            audience: JwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(JwtOptions.RefreshTokenExpirationDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<JwtToken?> GenerateJwtToken(RefreshToken refreshToken)
    {
        
        var validationResult = await ValidateSession(refreshToken);
        if(validationResult == null)
            return null;
        (Session session, RefreshToken newRefreshToken) = validationResult.Value;

        // Generate JWT token
        var secretKey = JwtOptions.SecretKey;
        var expiresMinutes = JwtOptions.ExpirationMinutes;

        var claims = new[]
        {
            new Claim("uid", session.UserId.ToString()),
            new Claim("sid", session.Id.ToString()),
            new Claim("role", "user")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtOptions.Issuer,
            audience: JwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            GenerateRefreshTokenJWT(newRefreshToken),
            "Bearer",
            expiresMinutes
        );
    }
    private string GenerateRefreshSecret()
    {
        var SecretSeed = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(SecretSeed);
    }

    public async Task<RefreshToken?> Login(string username, string password)
    {
        var user = await ValidateUser(username, password);
        
        if(user == null)
            return null;
        
        var RefreshSecret = GenerateRefreshSecret();

        Session session = new Session()
        {
            SecretHash = BCrypt.Net.BCrypt.HashPassword(RefreshSecret),
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            UserId = user.UserId,
            ExpireTime = DateTime.UtcNow.AddDays(JwtOptions.RefreshTokenExpirationDays)
        };  

        await MessengerDbContext.Sessions.AddAsync(session);
        await MessengerDbContext.SaveChangesAsync();

        return new RefreshToken(session.Id, RefreshSecret); 
    }

    public async Task<(Session, RefreshToken)?> ValidateSession(RefreshToken refreshToken)
    {
        var session = await MessengerDbContext.Sessions
            .FindAsync(refreshToken.SessionId);
        if(!VerifySession(session, refreshToken))
            return null;

        session!.LastActivityAt = DateTime.UtcNow;
        var newRefreshSecret = GenerateRefreshSecret();
        session.SecretHash = BCrypt.Net.BCrypt.HashPassword(newRefreshSecret);
        await MessengerDbContext.SaveChangesAsync();

        return (session, new RefreshToken(session.Id, newRefreshSecret));
    }

    public async Task<bool> Logout(String refreshJWTToken)
    {
        var refreshToken = GetRefreshTokenFromJWT(refreshJWTToken);
        if(refreshToken == null)
            return false;

        var session = await MessengerDbContext.Sessions
            .FindAsync(refreshToken.SessionId);
        
        if(!VerifySession(session, refreshToken))
            return false;
        
        session.RevokedAt = DateTime.UtcNow;
        await MessengerDbContext.SaveChangesAsync();

        return true;
    }
    public bool VerifySession(Session? session, RefreshToken refreshToken)
    {
        if(session == null)
            return false;

        if(session.ExpireTime <= DateTime.UtcNow)
            return false;
        
        if(session.RevokedAt != null)
            return false;

        if(!BCrypt.Net.BCrypt.Verify(refreshToken.Secret,session.SecretHash))
            return false;
        
        return true;
    }
    
}
