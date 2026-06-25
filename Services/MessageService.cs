using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implementation of message service for conversation and messaging operations.
    /// Enforces communication rules: no messaging before writer assignment;
    /// only client, assigned writer, and admins participate.
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageService(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Message> SendMessageAsync(int conversationId, string senderId, string messageText, int? attachmentId = null)
        {
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                MessageText = messageText,
                AttachmentId = attachmentId,
                CreatedDate = DateTime.UtcNow,
                IsRead = false,
                IsEdited = false
            };

            _context.Messages.Add(message);

            // Update conversation last message date
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<List<Message>> GetConversationMessagesAsync(int conversationId, int page = 1, int pageSize = 50)
        {
            return await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachment)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(int conversationId, string userId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                           m.SenderId != userId &&
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            // Update participant's last read date
            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);

            if (participant != null)
            {
                participant.LastReadDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadMessageCountAsync(string userId)
        {
            var participantConversationIds = await _context.ConversationParticipants
                .Where(p => p.UserId == userId)
                .Select(p => p.ConversationId)
                .ToListAsync();

            return await _context.Messages
                .Where(m => participantConversationIds.Contains(m.ConversationId) &&
                           m.SenderId != userId &&
                           !m.IsRead)
                .CountAsync();
        }

        public async Task<bool> HasAccessToConversationAsync(int conversationId, string userId)
        {
            // Check if user is a participant
            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId);

            if (isParticipant) return true;

            // Check if user is admin
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.UserType == "Administrator")
            {
                return true;
            }

            return false;
        }

        public async Task<Conversation> GetOrCreateConversationAsync(int orderId)
        {
            // Check if a conversation already exists for this order.
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.OrderId == orderId);

            if (existingConversation != null)
            {
                return existingConversation;
            }

            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.AssignedWriter)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                throw new InvalidOperationException($"Order with ID {orderId} not found.");
            }

            // ENFORCE MESSAGING RULE: Communication only allowed after writer assignment.
            // Before assignment, no conversation can be created.
            if (string.IsNullOrEmpty(order.AssignedWriterId))
            {
                throw new InvalidOperationException(
                    "Communication is not available until a writer has been assigned to this order.");
            }

            var conversation = new Conversation
            {
                OrderId = orderId,
                CreatedDate = DateTime.UtcNow,
                LastMessageDate = DateTime.UtcNow,
                IsArchived = false
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Add participants: client + assigned writer + admins.
            var participants = new List<ConversationParticipant>();

            // Add client as participant
            participants.Add(new ConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = order.ClientId,
                JoinedDate = DateTime.UtcNow,
                IsAdmin = false
            });

            // Add assigned writer as participant
            if (!string.IsNullOrEmpty(order.AssignedWriterId))
            {
                participants.Add(new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = order.AssignedWriterId,
                    JoinedDate = DateTime.UtcNow,
                    IsAdmin = false
                });
            }

            _context.ConversationParticipants.AddRange(participants);

            // Always add all administrators so they can view all conversations.
            var admins = await _userManager.GetUsersInRoleAsync("Administrator");
            foreach (var admin in admins)
            {
                _context.ConversationParticipants.Add(new ConversationParticipant
                {
                    ConversationId = conversation.Id,
                    UserId = admin.Id,
                    JoinedDate = DateTime.UtcNow,
                    IsAdmin = true
                });
            }

            await _context.SaveChangesAsync();

            return conversation;
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            var participantConversationIds = await _context.ConversationParticipants
                .Where(p => p.UserId == userId)
                .Select(p => p.ConversationId)
                .ToListAsync();

            return await _context.Conversations
                .Include(c => c.Order)
                .Where(c => participantConversationIds.Contains(c.Id) && !c.IsArchived)
                .OrderByDescending(c => c.LastMessageDate)
                .ToListAsync();
        }

        public async Task<Conversation?> GetConversationByIdAsync(int conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Order)
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task<int> GetUnreadConversationCountAsync(string userId)
        {
            var participantConversationIds = await _context.ConversationParticipants
                .Where(p => p.UserId == userId)
                .Select(p => p.ConversationId)
                .ToListAsync();

            return await _context.Messages
                .CountAsync(m => participantConversationIds.Contains(m.ConversationId)
                    && m.SenderId != userId
                    && !m.IsRead);
        }
    }
}
