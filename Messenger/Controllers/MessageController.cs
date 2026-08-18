using System.Security.Claims;
using DTO.Chat;
using Messenger.Realtime;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class MessageController(ILogger<UserController> logger, ChatService chatService, MessageService messageService, CurrentUser currentUser, RealtimeNotifier notifier) : ControllerBase
{
    [HttpPost("direct")]
    public async Task<IActionResult> Direct([FromBody] SendMessageRequest message, long receiverId, string? clientId)
    {
        var userId = currentUser.UserId;
        if(userId == receiverId)
            return BadRequest("Can't create direct chat with yourself");

        var chatCreation = await chatService.GetOrCreateDirectChat(userId, receiverId);
        if (chatCreation.Created)
        {
            var receiverPreview = await chatService.QueryChatPreview(receiverId, chatCreation.ChatId);

            if (receiverPreview != null)
                await notifier.NotifyUserAsync(receiverId, new { type = "new_chat", chat = receiverPreview });
        }
        var response = await messageService.SendMessage(chatCreation.ChatId, userId, message);
            

        return Ok(new {id = clientId, mes = response} );
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest message, long chatId, string? clientId)
    {
        var userId = currentUser.UserId;
        var response = await messageService.SendMessage(chatId, userId, message);
        return Ok(new {id = clientId, mes = response});
    }

    [HttpGet("get") ]
    public async Task<IActionResult> Get(long chatId, long? beforeMessageId, int PageSize)
    {
        var userId = currentUser.UserId;
        var response = await messageService.GetMessages(userId, chatId, beforeMessageId, PageSize);
        return Ok(response);
    }

    [HttpDelete("delete") ]
    public async Task<IActionResult> Delete(long chatId, long messageId)
    {
        var userId = currentUser.UserId;
        var response = await messageService.DeleteMessage(chatId, messageId, userId);
        if(response == null)
            return BadRequest();
        return Ok(response);
    }
    [HttpPatch("edittext") ]
    public async Task<IActionResult> EditText(long chatId, long messageId, [FromBody] EditTextRequest newText)
    {
        var userId = currentUser.UserId;
        var response = await messageService.EditTextMessage(chatId, messageId, userId, newText.NewText);
        if(response == null)
            return BadRequest();
        return Ok(response);
    }
}

public record EditTextRequest(string NewText);