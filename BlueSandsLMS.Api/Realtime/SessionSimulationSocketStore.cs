using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace BlueSandsLMS.Api.Realtime
{
    public sealed class SessionSimulationSocketStore
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _activeSockets = new();
        private readonly ConcurrentDictionary<Guid, string> _lastStates = new();

        public async Task RegisterAsync(Guid sessionId, WebSocket socket, ILogger logger, CancellationToken ct)
        {
            if (_activeSockets.TryGetValue(sessionId, out var existing) && existing.State == WebSocketState.Open)
            {
                try
                {
                    await existing.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Replaced by a newer session connection",
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not close existing socket for session {SessionId}", sessionId);
                }
            }

            _activeSockets[sessionId] = socket;
        }

        public void Remove(Guid sessionId, WebSocket socket)
        {
            if (_activeSockets.TryGetValue(sessionId, out var existing) && ReferenceEquals(existing, socket))
                _activeSockets.TryRemove(sessionId, out _);
        }

        public string? GetLastState(Guid sessionId) =>
            _lastStates.TryGetValue(sessionId, out var payload) ? payload : null;

        public void SetLastState(Guid sessionId, string payload) => _lastStates[sessionId] = payload;
    }
}
