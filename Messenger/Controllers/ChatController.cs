using DTO.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Messenger.Services;

namespace Messenger.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ChatController(ILogger<UserController> logger, ChatService chatService, CurrentUser currentUser) : ControllerBase
{
    [HttpGet("chats")]
    public async Task<IActionResult> Chats(int page, int pageSize, String? query)
    {
        var userId = currentUser.UserId;
        var response = await chatService.QueryChats(userId, page * pageSize, pageSize, query);
        return Ok(response);
    }

    [HttpGet("full")]
    public async Task<IActionResult> Full(long chatId)
    {
        var userId = currentUser.UserId;
        var response = await chatService.QueryChatInfo(userId, chatId);

        if (response == null)
            return NotFound();

        return Ok(response);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] ChatUpdateRequest request)
    {
        var userId = currentUser.UserId;
        var response = await chatService.UpdateChatInfo(userId, request.ChatId, request.DisplayName);

        if (response == null)
            return BadRequest("Only group chats can be edited.");

        return Ok(response);
    }

    [HttpPost("uploadavatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar([FromForm] long chatId, [FromForm] IFormFile avatar)
    {
        var userId = currentUser.UserId;
        var response = await chatService.UploadAvatar(userId, chatId, avatar);

        if (response == null)
            return BadRequest("Only group chats can be edited.");

        return Ok(response);
    }

    [HttpPost("group")]
    public async Task<IActionResult> CreateGroup([FromBody] ChatCreateRequest request)
    {
        var userId = currentUser.UserId;
        var response = await chatService.CreateGroupChat(userId, request.DisplayName, request.MemberIds);

        if (response == null)
            return BadRequest("Unable to create group chat.");

        return Ok(response);
    }
}
