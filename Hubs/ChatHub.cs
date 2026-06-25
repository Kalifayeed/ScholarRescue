using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Services;

namespace ScholarRescue.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat functionality.
    /// Handles messaging, conversation management, and user presence.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IUserPresenceService _presenceService;
        private readonly ScholarRescueDbContext _context;

        public ChatHub(
            IMessageService messageService,
            IUserPresenceService presenceService,
            ScholarRescueDbContext context)
        {
            _messageService = messageService;
            _presenceService = presenceService;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Joins a conversation room to receive real-time updates.
        /// </summary>
        public async Task JoinConversation(int conversationId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            // Validate access
            var hasAccess = await _messageService.HasAccessToConversationAsync(conversationId, userId);
            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this conversation.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());

            // Log audit
            var auditLog = new AuditLog
            {
                Action = "Conversation Joined",
                PerformedById = userId,
                Description = $"User joined conversation {conversationId}.",
                CreatedDate = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Notify others in the conversation
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserJoined", userId);
        }

        /// <summary>
        /// Leaves a conversation room.
        /// </summary>
        public async Task LeaveConversation(int conversationId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());

            // Log audit
            var auditLog = new AuditLog
            {
                Action = "Conversation Left",
                PerformedById = userId,
                Description = $"User left conversation {conversationId}.",
                CreatedDate = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Notify others in the conversation
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserLeft", userId);
        }

        /// <summary>
        /// Sends a message to a conversation.
        /// </summary>
        public async Task SendMessage(int conversationId, string messageText)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            // Validate access
            var hasAccess = await _messageService.HasAccessToConversationAsync(conversationId, userId);
            if (!hasAccess)
            {
                await Clients.Caller.SendAsync("Error", "You do not have access to this conversation.");
                return;
            }

            // Save message
            var message = await _messageService.SendMessageAsync(conversationId, userId, messageText);

            // Log audit
            var auditLog = new AuditLog
            {
                Action = "Message Sent",
                PerformedById = userId,
                Description = $"User sent message {message.Id} in conversation {conversationId}.",
                CreatedDate = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Get sender info
            var sender = await _context.Users.FindAsync(userId);
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown";

            // Broadcast to conversation group
            await Clients.Group(conversationId.ToString())
                .SendAsync("ReceiveMessage", new
            {
                Id = message.Id,
                ConversationId = conversationId,
                SenderId = userId,
                SenderName = senderName,
                MessageText = message.MessageText,
                CreatedDate = message.CreatedDate,
                IsRead = message.IsRead
            });

            // Create notifications for other participants
            var participants = await _context.ConversationParticipants
                .Where(p => p.ConversationId == conversationId && p.UserId != userId)
                .ToListAsync();

            foreach (var participant in participants)
            {
                var notification = new Notification
                {
                    UserId = participant.UserId,
                    Title = "New Message",
                    Message = $"{senderName} sent you a message",
                    NotificationType = Models.Enums.NotificationType.NewMessage,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    RelatedEntityId = conversationId.ToString()
                };

                _context.Notifications.Add(notification);

                // Send real-time notification
                var participantConnections = await _presenceService.GetUserConnectionIdsAsync(participant.UserId);
                foreach (var connectionId in participantConnections)
                {
                    await Clients.Client(connectionId)
                        .SendAsync("ReceiveNotification", new
                    {
                        Type = "NewMessage",
                        Title = "New Message",
                        Message = $"{senderName} sent you a message",
                        ConversationId = conversationId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Notifies other participants that the current user is typing.
        /// </summary>
        public async Task Typing(int conversationId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            var userName = Context.User?.Identity?.Name ?? "Someone";

            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    UserName = userName
                });
        }

        /// <summary>
        /// Marks messages in a conversation as read.
        /// </summary>
        public async Task MarkMessageRead(int conversationId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            await _messageService.MarkMessagesAsReadAsync(conversationId, userId);

            // Notify others that messages were read
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("MessagesRead", userId);
        }
    }
}