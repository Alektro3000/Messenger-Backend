using System.Security.Claims;
using DTO.Chat;
using DTO.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Messenger.Services;

namespace Messenger.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class UserController(ILogger<UserController> logger, UserService userService, CurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = currentUser.UserId;

        var user = await userService.QueryUser(userId);

        return Ok(user);
    }

    [HttpGet("query")]
    public async Task<IActionResult> Query(string? query, int page, int pageSize)
    {
        var userId = currentUser.UserId;
        var username = await userService.QueryUsers(query, userId, page * pageSize, pageSize);

        return Ok(username);
    }

    [HttpPost("uploadavatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar)
    {
        var userId = currentUser.UserId;
        try
        {
            var avatarUrl = await userService.UploadAvatar(userId, avatar);
            return Ok(new { url = avatarUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

    }
    [HttpPost("updateprofile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserUpdate newUser)
    {
        var userId = currentUser.UserId;
        var result = await userService.UpdateUser(userId, newUser);
        return Ok();

    }
}
