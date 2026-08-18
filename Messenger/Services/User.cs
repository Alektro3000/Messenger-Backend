using System.Linq.Expressions;
using DTO.Chat;
using DTO.User;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Repository;
using SkiaSharp;

namespace Messenger.Services;

public sealed class UserService(
    MessengerDbContext MessengerDbContext,
    ILogger<UserService> Logger,
    IWebHostEnvironment Environment)
{
    public static readonly Expression<Func<Models.User, UserFull>>
        UserToUserFull =
            x => new UserFull(
                x.Id,
                x.Username,
                x.DisplayName,
                x.Name,
                x.Surname,
                x.Bio,
                x.AvatarUrl,
                x.CreatedAt,
                x.LastSeenAt);
    public static readonly Expression<Func<Models.User, UserPreview>>
        UserToUserPreview =
            x => new UserPreview(
                x.Id,
                x.DisplayName,
                x.AvatarUrl,
                x.LastSeenAt);
    public Task<List<UserPreview>> QueryUsers(string? query, long UserId, int offset, int limit)
    {
        return MessengerDbContext.Users
            .AsNoTracking()
            .Where(x =>
                x.Id != UserId && (query == null || 
                x.Username.Contains(query) ||
                x.DisplayName.Contains(query) ||
                (x.Name != null && x.Name.Contains(query)) ||
                (x.Surname != null && x.Surname.Contains(query))))
            .OrderBy(x=>x.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(UserToUserPreview)
            .ToListAsync();
    }
    public Task<UserFull?> QueryUser(long UserId)
    {
        return MessengerDbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == UserId)
            .Select(UserToUserFull)
            .SingleOrDefaultAsync();
    }
    public async Task<ChangeProfileResult> UpdateUser(long currentUserId, UserUpdate newUser)
    {
        var user = await MessengerDbContext.Users
            .FindAsync(currentUserId);

        if (user == null)
            return ChangeProfileResult.UserNotFound;
        try
        {
            var newName = string.IsNullOrWhiteSpace(newUser.Name) ? null : newUser.Name;
            var newSurname =  string.IsNullOrWhiteSpace(newUser.Surname) ? null : newUser.Surname;

            user.Name = newName;
            user.Surname = newSurname;
            user.Bio = newUser.Bio;
            
            var allAvailable = newName != null && newSurname != null;
            
            user.DisplayName = allAvailable
                ? newSurname + " " + newName
                : newName ?? newSurname ?? user.Username;
            await MessengerDbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pgEx &&
                pgEx.SqlState == PostgresErrorCodes.UniqueViolation &&
                pgEx.ConstraintName == "UniqueIndex_Users_UserName")
        {
            Logger.LogWarning(ex,
                "Changing profile failed because username {Username} already exists.",
                user.Username);

            return ChangeProfileResult.RepeatedUsername;
        }
        return ChangeProfileResult.Succesful;
    }
    public async Task<string?> UploadAvatar(long userId, IFormFile avatar)
    {
        if (avatar == null || avatar.Length == 0)
            return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();

        var fileName = $"user-{userId}-{Guid.NewGuid()}{extension}";
        var folder = Path.Combine(Environment.WebRootPath, "uploads", "avatars");

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Unsupported file type");

        var user = await MessengerDbContext.Users
            .FindAsync(userId);

        if (user == null)
            return null;

        var oldUrl = user.AvatarUrl;

        Directory.CreateDirectory(folder);
        var newPath = Path.Combine(folder, fileName);

        try
        {
            await using var validateStream = avatar.OpenReadStream();
            using var codec = SKCodec.Create(validateStream) ?? throw new ArgumentException("Invalid image");
            
            if (codec.Info.Width != codec.Info.Height)
                throw new ArgumentException("Avatar must be square");
            
            if (codec.Info.Width > 1024 || codec.Info.Height > 1024)
                throw new ArgumentException("Image too large");

            await using var inputStream = avatar.OpenReadStream();

            await using var outputStream = File.Create(newPath);
            await inputStream.CopyToAsync(outputStream);

            //Update DB
            var newUri = $"/uploads/avatars/{fileName}";
            user.AvatarUrl = newUri;
            await MessengerDbContext.SaveChangesAsync();

            //Delete old file
            if (oldUrl != null)
            {
                var oldPathSuffix = oldUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var oldPath = Path.Combine(Environment.WebRootPath, oldPathSuffix);
                try
                {
                    if (File.Exists(oldPath))
                        File.Delete(oldPath);
                }
                catch
                {
                    Logger.LogWarning("Failed to delete old avatar {oldPath}", oldPath);
                }
            }

            return newUri;
        }
        catch
        {
            // Rollback new file
            if (File.Exists(newPath))
                File.Delete(newPath);

            // Restore DB
            user.AvatarUrl = oldUrl ;

            throw;
        }
    }
}