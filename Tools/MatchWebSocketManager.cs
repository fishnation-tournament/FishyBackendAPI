using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FishyAPI.Tools;
public static class MatchWebSocketManager
{
    private static readonly ConcurrentDictionary<ulong, List<WebSocket>> _connections = new();
    private static readonly object _lock = new();
    // Same camelCase naming as the REST responses.
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public static void AddConnection(ulong matchId, WebSocket socket)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(matchId, out var list))
            {
                list = new List<WebSocket>();
                _connections[matchId] = list;
            }
            list.Add(socket);
            Console.WriteLine($"[WS] Connection added for match {matchId} — total for match: {list.Count}");
        }
    }

    public static void RemoveConnection(ulong matchId, WebSocket socket)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(matchId, out var list))
            {
                list.Remove(socket);
                if (list.Count == 0)
                    _connections.TryRemove(matchId, out _);
            }
            Console.WriteLine($"[WS] Connection removed for match {matchId}");
        }
    }

    public static async Task BroadcastToMatch(ulong matchId, string eventType, object data, JsonSerializerOptions? options = null)
    {
        Console.WriteLine($"[WS] BroadcastToMatch called — matchId={matchId}, event={eventType}, connections={_connections.Count}");
        List<WebSocket> snapshot;
        lock (_lock)
        {
            if (!_connections.TryGetValue(matchId, out var list))
            {
                Console.WriteLine($"[WS] No connections registered for match {matchId}");
                return;
            }
            snapshot = list.ToList();
        }
        Console.WriteLine($"[WS] Broadcasting to {snapshot.Count} socket(s) for match {matchId}");

        string payload;
        try
        {
            payload = JsonSerializer.Serialize(new { @event = eventType, matchId, data }, options ?? WebJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] Serialization failed for event '{eventType}' on match {matchId}: {ex.Message}");
            return;
        }
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);

        List<WebSocket> dead = new();
        foreach (var ws in snapshot)
        {
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    dead.Add(ws);
                }
            }
            else
            {
                dead.Add(ws);
            }
        }

        if (dead.Count > 0)
        {
            lock (_lock)
            {
                if (_connections.TryGetValue(matchId, out var list))
                    foreach (var ws in dead)
                        list.Remove(ws);
            }
        }
    }

    public static async Task HandleConnection(ulong matchId, WebSocket socket)
    {
        AddConnection(matchId, socket);
        try
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (WebSocketException)
        {
            // Connection dropped unexpectedly.
        }
        finally
        {
            RemoveConnection(matchId, socket);
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None); }
                catch { /* ignore */ }
            }
        }
    }
}
