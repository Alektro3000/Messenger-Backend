using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Messenger.Realtime;

public class RealtimeNotifier(
    WebSocketConnectionManager connections,
    ILogger<RealtimeNotifier> logger
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private static ArraySegment<byte> SerializeJson(object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return bytes;
    }

    public Task NotifyUserAsync(long userId, object payload, CancellationToken ct = default)
    {
        return connections.SendAsync(userId,  SerializeJson(payload), ct);
    }

    public async Task NotifyUsersAsync(IEnumerable<long> userIds, object payload, CancellationToken ct = default)
    {
        try
        {
            var bytes = SerializeJson(payload);
            await Task.WhenAll(
                userIds.Select(id => {
                    return connections.SendAsync(id, bytes, ct);
                })
            );
        }
        catch (Exception ex)
        {
            // log, but don't fail SendMessage
            logger.LogError(ex, "Failed to Notify Users");
        }
    }
}