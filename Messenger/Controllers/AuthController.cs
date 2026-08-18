
using DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Messenger.Services;

namespace Messenger.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(AuthService authService, ILogger<AuthController> logger) : ControllerBase
{

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest   request)
    {
        var refreshToken = await authService.Login(request.Username, request.Password);
        if (refreshToken == null)
            return Unauthorized("Invalid credentials");
        
        var token = await authService.GenerateJwtToken(refreshToken);
        if (token == null)
        {
            logger.LogError("Failed to generate JWT token for session {SessionId} in login", refreshToken.SessionId);
            return Unauthorized("Invalid session"); // This should not happen, but we check it just in case
        }
        return Ok(token);
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest refresh)
    {        
        var refreshTokenObj = authService.GetRefreshTokenFromJWT(refresh.RefreshToken);
        if (refreshTokenObj == null)
            return BadRequest("Invalid refresh token format");

        var token = await authService.GenerateJwtToken(refreshTokenObj);
        if (token == null)
            return Unauthorized("Invalid session");

        return Ok(token);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterUser(request.Username, request.Password);
        
        if (result == RegisterResult.Succesful)
        {
            return NoContent();
        }
        
        if (result == RegisterResult.Succesful)
        {
            return BadRequest("User with this username already exist");
        }
        return BadRequest("Failed to register user");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest logout)
    {
        var result = await authService.Logout(logout.RefreshToken);
        if (result)
        {
            return NoContent();
        }
        return BadRequest("Failed to logout user");
    }
}

public sealed record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public sealed record RegisterRequest(string Username, string Password);