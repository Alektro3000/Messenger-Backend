using System.Net.WebSockets;

namespace Messenger.Realtime;
public sealed class WebSocketHandler(
    WebSocketConnectionManager connections,
    ILogger<WebSocketHandler> logger)
{

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var sessionIdString = context.User.FindFirst("sid")?.Value;

        if (sessionIdString is null || !long.TryParse(sessionIdString, out var sessionId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userIdString = context.User.FindFirst("uid")?.Value;

        if (userIdString is null || !long.TryParse(userIdString, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }


        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        connections.Add(userId, sessionId, socket);

        try
        {
            await WaitUntilSocketClosesAsync(socket, context.RequestAborted);
        }
        finally
        {
            connections.Remove(userId, sessionId);
        }
    }

    private async Task WaitUntilSocketClosesAsync(
        WebSocket socket,
        CancellationToken ct)
    {
        var buffer = new byte[1024];

        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            try
            {
                var result = await socket.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by client",
                        ct
                    );

                    break;
                }
            }
            catch(OperationCanceledException)
            {
                //Do nothing
                break;
            }
            catch (WebSocketException ex)
            {
                // Normal-ish: client disconnected without proper close frame
                logger.LogDebug(ex, "WebSocket disconnected unexpectedly.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected WebSocket error.");
            }

            // We ignore messages from client for now.
            // This loop exists mostly to detect disconnect.
        }
    }
}