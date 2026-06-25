using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Services;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Temporary controller for testing messaging backend functionality.
    /// Protect with authorization - remove or disable in production.
    /// </summary>
    [Authorize]
    public class MessagingTestController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagingTestController(
            IMessageService messageService,
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _messageService = messageService;
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Lists all conversations for the current user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Conversations()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var conversations = await _messageService.GetUserConversationsAsync(userId);
            return View(conversations);
        }

        /// <summary>
        /// Lists messages in a specific conversation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Messages(int conversationId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var hasAccess = await _messageService.HasAccessToConversationAsync(conversationId, userId);
            if (!hasAccess) return Forbid();

            var messages = await _messageService.GetConversationMessagesAsync(conversationId);
            return View(messages);
        }
    }
}