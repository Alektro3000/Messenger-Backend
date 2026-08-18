using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Messenger.Realtime;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<long, WebSocket>> _sockets = new();

    public void Add(long userId, long sessionId, WebSocket socket)
    {
        var sessions = _sockets.GetOrAdd(
            userId,
            _ => new ConcurrentDictionary<long, WebSocket>()
        );
        sessions[sessionId] = socket;
    }

    public void Remove(long userId, long sessionId)
    {
        if (!_sockets.TryGetValue(userId, out var sessions))
            return;

        sessions.TryRemove(sessionId, out _);

        if (sessions.IsEmpty)
        {
            _sockets.TryRemove(userId, out _);
        }
    }

    public async Task SendAsync(
        long userId,
        ArraySegment<byte> bytes,
        CancellationToken ct = default)
    {
        if (!_sockets.TryGetValue(userId, out var socketBag))
            return;

        foreach (var (sessionId, socket) in socketBag)
        {
            if (socket.State != WebSocketState.Open)
            {
                Remove(userId, sessionId);
            }

            try
            {
                await socket.SendAsync(
                    bytes,
                    WebSocketMessageType.Text,
                    true,
                    ct
                );
            }
            catch (WebSocketException)
            {
                Remove(userId, sessionId);
            }
        }
    }
}