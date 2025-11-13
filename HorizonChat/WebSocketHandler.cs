using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace HorizonChat;

public class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();

    public async Task HandleWebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var username = context.Request.Query["username"].ToString();
        if (string.IsNullOrEmpty(username))
        {
            username = "Guest";
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        var connectionInfo = new ConnectionInfo
        {
            WebSocket = webSocket,
            Username = username,
            ConnectedAt = DateTime.Now
        };
        
        // Handle Connect Event
        if (_connections.TryAdd(connectionId, connectionInfo))
        {
            Console.WriteLine($"[CONNECT] {username} (ID: {connectionId}) connected at {connectionInfo.ConnectedAt:HH:mm:ss}");
            
            // Send join notification to all clients
            await BroadcastMessage(new ChatMessage
            {
                Username = "System",
                Content = $"{username} joined the chat",
                Timestamp = DateTime.Now
            });
        }

        try
        {
            await ReceiveMessages(webSocket, connectionId, username);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Connection {connectionId} ({username}): {ex.Message}");
        }
        finally
        {
            // Handle Disconnect Event
            if (_connections.TryRemove(connectionId, out var removedConnection))
            {
                var duration = DateTime.Now - removedConnection.ConnectedAt;
                Console.WriteLine($"[DISCONNECT] {username} (ID: {connectionId}) disconnected. Duration: {duration.TotalMinutes:F1} minutes");
                
                // Send leave notification if connection wasn't aborted
                if (webSocket.State != WebSocketState.Aborted)
                {
                    await BroadcastMessage(new ChatMessage
                    {
                        Username = "System",
                        Content = $"{username} left the chat",
                        Timestamp = DateTime.Now
                    });
                }
            }

            // Ensure WebSocket is properly closed
            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed",
                    CancellationToken.None);
            }
            
            webSocket.Dispose();
        }
    }

    private async Task ReceiveMessages(WebSocket webSocket, string connectionId, string username)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            // Handle Close Event
            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine($"[CLOSE] Close frame received from {username} (ID: {connectionId}). Status: {result.CloseStatus}, Reason: {result.CloseStatusDescription}");
                await webSocket.CloseAsync(
                    result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    result.CloseStatusDescription ?? "Client closed connection",
                    CancellationToken.None);
                break;
            }

            // Handle Message Event
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                try
                {
                    var message = JsonSerializer.Deserialize<ChatMessage>(messageJson);
                    
                    if (message != null && !string.IsNullOrWhiteSpace(message.Content))
                    {
                        message.Timestamp = DateTime.Now;
                        Console.WriteLine($"[MESSAGE] {username}: {message.Content}");
                        
                        // Broadcast to ALL connected clients (including sender)
                        await BroadcastMessage(message);
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] Empty or invalid message from {username}");
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ERROR] JSON parse error from {username}: {ex.Message}");
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                Console.WriteLine($"[WARNING] Binary message received from {username}, ignoring");
            }
        }
    }

    private async Task BroadcastMessage(ChatMessage message, string? excludeConnectionId = null)
    {
        var messageJson = JsonSerializer.Serialize(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);
        
        var successCount = 0;
        var failCount = 0;

        var tasks = _connections
            .Where(c => (excludeConnectionId == null || c.Key != excludeConnectionId) && c.Value.WebSocket.State == WebSocketState.Open)
            .Select(async c =>
            {
                try
                {
                    await c.Value.WebSocket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to send to {c.Value.Username}: {ex.Message}");
                    Interlocked.Increment(ref failCount);
                }
            });

        await Task.WhenAll(tasks);
        
        Console.WriteLine($"[BROADCAST] Message sent to {successCount} clients, {failCount} failed");
    }

    public static int GetActiveConnectionCount() => _connections.Count;

    public static IEnumerable<string> GetConnectedUsers() => _connections.Values.Select(c => c.Username);

    private class ConnectionInfo
    {
        public required WebSocket WebSocket { get; set; }
        public required string Username { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public class ChatMessage
    {
        public string Username { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
