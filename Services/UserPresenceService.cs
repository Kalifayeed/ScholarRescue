using System.Collections.Concurrent;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implementation of user presence tracking using in-memory storage.
    /// </summary>
    public class UserPresenceService : IUserPresenceService
    {
        // userId -> set of connectionIds
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _userConnections = new();

        // userId -> last seen timestamp
        private static readonly ConcurrentDictionary<string, DateTime> _lastSeen = new();

        public Task UserConnectedAsync(string userId, string connectionId)
        {
            var connections = _userConnections.GetOrAdd(userId, _ => new ConcurrentBag<string>());
            connections.Add(connectionId);
            _lastSeen[userId] = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task UserDisconnectedAsync(string userId, string connectionId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                // Create a new bag without the disconnected connection
                var newConnections = new ConcurrentBag<string>(connections.Where(c => c != connectionId));
                _userConnections[userId] = newConnections;

                // If no more connections, mark as offline
                if (newConnections.IsEmpty)
                {
                    _lastSeen[userId] = DateTime.UtcNow;
                }
            }
            return Task.CompletedTask;
        }

        public bool IsOnline(string userId)
        {
            return _userConnections.TryGetValue(userId, out var connections) && !connections.IsEmpty;
        }

        public Task<List<string>> GetOnlineUsersAsync()
        {
            var onlineUsers = _userConnections
                .Where(kvp => !kvp.Value.IsEmpty)
                .Select(kvp => kvp.Key)
                .ToList();

            return Task.FromResult(onlineUsers);
        }

        public Task<List<string>> GetUserConnectionIdsAsync(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return Task.FromResult(connections.ToList());
            }

            return Task.FromResult(new List<string>());
        }

        public Task<DateTime?> GetLastSeenAsync(string userId)
        {
            if (_lastSeen.TryGetValue(userId, out var lastSeen))
            {
                return Task.FromResult<DateTime?>(lastSeen);
            }

            return Task.FromResult<DateTime?>(null);
        }
    }
}