namespace ScholarRescue.Services
{
    /// <summary>
    /// Service interface for tracking user online presence.
    /// </summary>
    public interface IUserPresenceService
    {
        /// <summary>
        /// Records that a user has connected.
        /// </summary>
        Task UserConnectedAsync(string userId, string connectionId);

        /// <summary>
        /// Records that a user has disconnected.
        /// </summary>
        Task UserDisconnectedAsync(string userId, string connectionId);

        /// <summary>
        /// Checks if a user is currently online.
        /// </summary>
        bool IsOnline(string userId);

        /// <summary>
        /// Gets all online user IDs.
        /// </summary>
        Task<List<string>> GetOnlineUsersAsync();

        /// <summary>
        /// Gets connection IDs for a specific user.
        /// </summary>
        Task<List<string>> GetUserConnectionIdsAsync(string userId);

        /// <summary>
        /// Gets the last seen timestamp for a user.
        /// </summary>
        Task<DateTime?> GetLastSeenAsync(string userId);
    }
}