using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service interface for message and conversation operations.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Creates a new message in a conversation.
        /// </summary>
        Task<Message> SendMessageAsync(int conversationId, string senderId, string messageText, int? attachmentId = null);

        /// <summary>
        /// Gets messages for a conversation with pagination.
        /// </summary>
        Task<List<Message>> GetConversationMessagesAsync(int conversationId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Marks all messages in a conversation as read for a specific user.
        /// </summary>
        Task MarkMessagesAsReadAsync(int conversationId, string userId);

        /// <summary>
        /// Gets the number of unread messages for a user across all conversations.
        /// </summary>
        Task<int> GetUnreadMessageCountAsync(string userId);

        /// <summary>
        /// Validates if a user has access to a conversation.
        /// </summary>
        Task<bool> HasAccessToConversationAsync(int conversationId, string userId);

        /// <summary>
        /// Gets or creates a conversation for an order.
        /// </summary>
        Task<Conversation> GetOrCreateConversationAsync(int orderId);

        /// <summary>
        /// Gets conversations for a user.
        /// </summary>
        Task<List<Conversation>> GetUserConversationsAsync(string userId);

        /// <summary>
        /// Gets a conversation by ID.
        /// </summary>
        Task<Conversation?> GetConversationByIdAsync(int conversationId);

        /// <summary>
        /// Gets the count of conversations with unread messages for a user.
        /// </summary>
        Task<int> GetUnreadConversationCountAsync(string userId);
    }
}