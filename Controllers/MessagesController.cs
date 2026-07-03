using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Services;
using ScholarRescue.ViewModels.Messaging;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Production controller powering the real-time messaging UI.
    /// Provides the conversation list, conversation details (chat) view,
    /// message send endpoint, search, and dashboard data APIs.
    /// Now also supports department support ticketing (Part 12D).
    /// Backed by <see cref="IMessageService"/>, <see cref="ISupportTicketService"/>, and SignalR <c>ChatHub</c>.
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class MessagesController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly ISupportTicketService _ticketService;
        private readonly IUserPresenceService _presenceService;
        private readonly IFileScanningService _fileScanningService;
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MessagesController> _logger;

        private static readonly string[] AllowedExtensions = {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
            ".txt", ".csv", ".zip", ".rar", ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };
        private const long MaxFileSize = 25L * 1024 * 1024; // 25 MB
        private const int MaxFiles = 10;

        public MessagesController(
            IMessageService messageService,
            ISupportTicketService ticketService,
            IUserPresenceService presenceService,
            IFileScanningService fileScanningService,
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _ticketService = ticketService;
            _presenceService = presenceService;
            _fileScanningService = fileScanningService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // CONVERSATION LIST (Now combined with support tickets)
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Messages
        /// Lists conversations and support tickets for the current user.
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? search, SupportDepartment? department, TicketStatus? status)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                bool isAdmin = User.IsInRole(RoleNames.Administrator);

                var model = new MessagingCenterViewModel
                {
                    SearchTerm = search
                };

                // Load order conversations
                var conversations = await _messageService.GetUserConversationsAsync(currentUser.Id);

                if (isAdmin)
                {
                    var allConvs = await _context.Conversations
                        .Include(c => c.Order)
                        .Where(c => !c.IsArchived)
                        .OrderByDescending(c => c.LastMessageDate)
                        .ToListAsync();

                    var existingIds = conversations.Select(c => c.Id).ToHashSet();
                    foreach (var c in allConvs.Where(c => !existingIds.Contains(c.Id)))
                    {
                        conversations.Add(c);
                    }
                    conversations = conversations.OrderByDescending(c => c.LastMessageDate).ToList();
                }

                // Build conversation list items
                foreach (var conv in conversations)
                {
                    var item = await BuildConversationListItemAsync(conv, currentUser);
                    if (item == null) continue;
                    model.Conversations.Add(item);
                }

                // Load support tickets
                var tickets = await _ticketService.GetTicketsAsync(
                    isAdmin ? null : currentUser.Id,
                    department,
                    status);

                foreach (var ticket in tickets)
                {
                    model.SupportTickets.Add(new TicketListViewModel
                    {
                        Id = ticket.Id,
                        TicketNumber = ticket.TicketNumber,
                        Subject = ticket.Subject,
                        Department = ticket.Department,
                        DepartmentDisplay = SupportTicketService.GetDepartmentDisplayName(ticket.Department),
                        Status = ticket.Status,
                        StatusDisplay = ticket.Status.ToString(),
                        Priority = ticket.Priority,
                        CreatedAt = ticket.CreatedAt,
                        LastReplyDate = ticket.LastReplyDate,
                        UnreadCount = ticket.UnreadCount,
                        QueueName = ticket.QueueName,
                        HasAttachments = ticket.Attachments != null && ticket.Attachments.Any()
                    });
                }

                // Server-side search filtering
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    model.Conversations = model.Conversations
                        .Where(c =>
                            c.OrderNumber.ToLowerInvariant().Contains(term) ||
                            c.OrderTitle.ToLowerInvariant().Contains(term) ||
                            c.OtherPartyName.ToLowerInvariant().Contains(term) ||
                            (c.LastMessagePreview ?? string.Empty).ToLowerInvariant().Contains(term))
                        .ToList();

                    model.SupportTickets = model.SupportTickets
                        .Where(t =>
                            t.TicketNumber.ToLowerInvariant().Contains(term) ||
                            t.Subject.ToLowerInvariant().Contains(term) ||
                            t.DepartmentDisplay.ToLowerInvariant().Contains(term))
                        .ToList();
                }

                model.TotalUnreadCount = await _messageService.GetUnreadMessageCountAsync(currentUser.Id);
                model.OpenTicketCount = await _ticketService.GetOpenTicketCountAsync(currentUser.Id);

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading messaging center.");
                TempData["ErrorMessage"] = "An error occurred while loading messages.";
                return View("Index", new MessagingCenterViewModel());
            }
        }

        // ----------------------------------------------------------------
        // CREATE SUPPORT TICKET (Department Message)
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Messages/NewMessage
        /// Shows the department messaging form.
        /// </summary>
        [HttpGet("NewMessage")]
        public IActionResult NewMessage()
        {
            return View(new CreateSupportMessageViewModel());
        }

        /// <summary>
        /// POST: /Messages/NewMessage
        /// Creates a new support ticket from the department messaging form.
        /// </summary>
        [HttpPost("NewMessage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewMessage(CreateSupportMessageViewModel model)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                // Validate
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var subject = (model.Subject ?? string.Empty).Trim();
                var message = (model.Message ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(subject))
                {
                    ModelState.AddModelError("Subject", "Subject is required.");
                    return View(model);
                }
                if (subject.Length > 200)
                {
                    ModelState.AddModelError("Subject", "Subject cannot exceed 200 characters.");
                    return View(model);
                }
                if (string.IsNullOrEmpty(message))
                {
                    ModelState.AddModelError("Message", "Message is required.");
                    return View(model);
                }
                if (message.Length < 10)
                {
                    ModelState.AddModelError("Message", "Message must be at least 10 characters.");
                    return View(model);
                }
                if (message.Length > 10000)
                {
                    ModelState.AddModelError("Message", "Message cannot exceed 10,000 characters.");
                    return View(model);
                }

                // Create the ticket
                var ticket = await _ticketService.CreateTicketAsync(
                    subject, message, model.Department, currentUser.Id, null);

                // Handle file attachments
                if (model.Attachments != null && model.Attachments.Count > 0)
                {
                    int fileCount = 0;
                    foreach (var file in model.Attachments)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (fileCount >= MaxFiles) break;

                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!AllowedExtensions.Contains(ext))
                        {
                            TempData["WarningMessage"] = $"File '{file.FileName}' has an unsupported format and was skipped.";
                            continue;
                        }
                        if (file.Length > MaxFileSize)
                        {
                            TempData["WarningMessage"] = $"File '{file.FileName}' exceeds 25MB and was skipped.";
                            continue;
                        }

                        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "support-tickets", ticket.Id.ToString());
                        Directory.CreateDirectory(dir);
                        var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{file.FileName}";
                        var filePath = Path.Combine(dir, safeName);

                        // Write file first, then scan
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var scanResult = await _fileScanningService.ScanFileAsync(filePath, file.FileName, currentUser.Id);
                        if (scanResult.IsBlocked)
                        {
                            System.IO.File.Delete(filePath);
                            TempData["WarningMessage"] = $"File '{file.FileName}' failed security scan and was skipped.";
                            continue;
                        }

                        await _ticketService.AddAttachmentAsync(
                            ticket.Id, file.FileName, $"/uploads/support-tickets/{ticket.Id}/{safeName}", file.Length, currentUser.Id);
                        fileCount++;
                    }
                }

                TempData["SuccessMessage"] = $"Your support request has been submitted. Ticket #{ticket.TicketNumber}";
                return RedirectToAction(nameof(TicketDetail), new { id = ticket.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating support ticket.");
                ModelState.AddModelError("", "An error occurred while creating your ticket. Please try again.");
                return View(model);
            }
        }

        // ----------------------------------------------------------------
        // TICKET DETAIL / THREAD VIEW
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Messages/Ticket/5
        /// Displays a support ticket and its conversation thread.
        /// </summary>
        [HttpGet("Ticket/{id:int}")]
        public async Task<IActionResult> TicketDetail(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var ticket = await _ticketService.GetByIdAsync(id);
                if (ticket == null) return NotFound();

                bool isAdmin = User.IsInRole(RoleNames.Administrator);
                if (!isAdmin && ticket.CreatorId != currentUser.Id) return Forbid();

                var notes = await _ticketService.GetNotesAsync(id);
                var attachments = await _ticketService.GetAttachmentsAsync(id);

                // Mark ticket as read if viewing as creator
                if (ticket.CreatorId == currentUser.Id)
                {
                    await _ticketService.MarkTicketAsReadAsync(id, currentUser.Id);
                }

                var model = new TicketThreadViewModel
                {
                    Id = ticket.Id,
                    TicketNumber = ticket.TicketNumber,
                    Subject = ticket.Subject,
                    Description = ticket.Description,
                    Department = ticket.Department,
                    DepartmentDisplay = SupportTicketService.GetDepartmentDisplayName(ticket.Department),
                    Status = ticket.Status,
                    StatusDisplay = ticket.Status.ToString(),
                    Priority = ticket.Priority,
                    CreatedAt = ticket.CreatedAt,
                    CreatorName = ticket.Creator != null
                        ? $"{ticket.Creator.FirstName} {ticket.Creator.LastName}".Trim()
                        : "Unknown",
                    AssignedAdminName = ticket.AssignedAdmin != null
                        ? $"{ticket.AssignedAdmin.FirstName} {ticket.AssignedAdmin.LastName}".Trim()
                        : null,
                    QueueName = ticket.QueueName,
                    IsAdmin = isAdmin,
                    Notes = notes.Select(n => new TicketNoteViewModel
                    {
                        Id = n.Id,
                        Content = n.Content,
                        AuthorName = n.Author != null
                            ? $"{n.Author.FirstName} {n.Author.LastName}".Trim()
                            : "Unknown",
                        AuthorId = n.AuthorId,
                        CreatedAt = n.CreatedAt,
                        IsAdminNote = n.Author != null && n.Author.UserType == RoleNames.Administrator,
                        IsInternal = n.IsInternal
                    }).ToList(),
                    Attachments = attachments.Select(a => new TicketAttachmentViewModel
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedAt = a.UploadedAt
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ticket {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred while loading the ticket.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Messages/Ticket/5/Reply
        /// Adds a reply to a support ticket.
        /// </summary>
        [HttpPost("Ticket/{id:int}/Reply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TicketReply(int id, string content, List<IFormFile>? attachments)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();

                bool isAdmin = User.IsInRole(RoleNames.Administrator);
                if (!isAdmin && ticket.CreatorId != currentUser.Id) return Forbid();

                if (string.IsNullOrWhiteSpace(content))
                {
                    TempData["ErrorMessage"] = "Reply content is required.";
                    return RedirectToAction(nameof(TicketDetail), new { id });
                }

                // Add the note/reply
                await _ticketService.AddNoteAsync(id, content.Trim(), currentUser.Id);

                // Handle attachment uploads
                if (attachments != null && attachments.Count > 0)
                {
                    int fileCount = 0;
                    foreach (var file in attachments)
                    {
                        if (file == null || file.Length == 0) continue;
                        if (fileCount >= MaxFiles) break;

                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!AllowedExtensions.Contains(ext)) continue;
                        if (file.Length > MaxFileSize) continue;

                        var dir2 = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "support-tickets", id.ToString());
                        Directory.CreateDirectory(dir2);
                        var safeName2 = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{file.FileName}";
                        var filePath2 = Path.Combine(dir2, safeName2);

                        using (var stream2 = new FileStream(filePath2, FileMode.Create))
                        {
                            await file.CopyToAsync(stream2);
                        }

                        var scanResult2 = await _fileScanningService.ScanFileAsync(filePath2, file.FileName, currentUser.Id);
                        if (scanResult2.IsBlocked)
                        {
                            System.IO.File.Delete(filePath2);
                            continue;
                        }

                        await _ticketService.AddAttachmentAsync(id, file.FileName, $"/uploads/support-tickets/{id}/{safeName2}", file.Length, currentUser.Id);
                        fileCount++;
                    }
                }

                TempData["SuccessMessage"] = "Your reply has been added.";
                return RedirectToAction(nameof(TicketDetail), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to ticket {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred while replying.";
                return RedirectToAction(nameof(TicketDetail), new { id });
            }
        }

        /// <summary>
        /// POST: /Messages/Ticket/5/Reopen
        /// Reopens a resolved/closed ticket.
        /// </summary>
        [HttpPost("Ticket/{id:int}/Reopen")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenTicket(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var ticket = await _context.SupportTickets.FindAsync(id);
                if (ticket == null) return NotFound();

                bool isAdmin = User.IsInRole(RoleNames.Administrator);
                if (!isAdmin && ticket.CreatorId != currentUser.Id) return Forbid();

                if (ticket.Status != TicketStatus.Resolved && ticket.Status != TicketStatus.Closed)
                {
                    TempData["ErrorMessage"] = "Only resolved or closed tickets can be reopened.";
                    return RedirectToAction(nameof(TicketDetail), new { id });
                }

                await _ticketService.ReopenTicketAsync(id, currentUser.Id);
                TempData["SuccessMessage"] = "Ticket has been reopened.";
                return RedirectToAction(nameof(TicketDetail), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening ticket {Id}.", id);
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction(nameof(TicketDetail), new { id });
            }
        }

        // ----------------------------------------------------------------
        // SEARCH TICKETS
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Messages/TicketSearch?q=term
        /// Searches support tickets by number, subject, department, status.
        /// </summary>
        [HttpGet("TicketSearch")]
        public async Task<IActionResult> TicketSearch(string? q)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                bool isAdmin = User.IsInRole(RoleNames.Administrator);
                var tickets = string.IsNullOrWhiteSpace(q)
                    ? new List<SupportTicket>()
                    : await _ticketService.SearchTicketsAsync(q, isAdmin ? null : currentUser.Id);

                var result = tickets.Select(t => new
                {
                    id = t.Id,
                    ticketNumber = t.TicketNumber,
                    subject = t.Subject,
                    department = t.Department.ToString(),
                    status = t.Status.ToString(),
                    createdAt = t.CreatedAt
                });

                return Ok(new { success = true, tickets = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching tickets.");
                return StatusCode(500, new { success = false });
            }
        }

        // ----------------------------------------------------------------
        // EXISTING METHODS (Preserved for backward compatibility)
        // ----------------------------------------------------------------

        /// <summary>
        /// GET: /Messages/Conversation/{id}
        /// Renders the chat interface for order conversations.
        /// </summary>
        [HttpGet("Conversation/{id:int}")]
        public async Task<IActionResult> Conversation(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!await _messageService.HasAccessToConversationAsync(id, currentUser.Id))
                    return Forbid();

                var conv = await _messageService.GetConversationByIdAsync(id);
                if (conv == null) return NotFound();

                conv = await _messageService.GetOrCreateConversationAsync(conv.OrderId);
                var messages = await _messageService.GetConversationMessagesAsync(id, 1, 200);

                var model = new ConversationDetailsViewModel
                {
                    Id = conv.Id,
                    OrderId = conv.OrderId,
                    OrderNumber = conv.Order?.OrderNumber ?? string.Empty,
                    OrderTitle = conv.Order?.Title ?? string.Empty,
                    CurrentUserId = currentUser.Id,
                    CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                    CurrentUserRole = currentUser.UserType,
                    CanAccess = true,
                    TotalMessages = messages.Count
                };

                var participants = await _context.ConversationParticipants
                    .Include(p => p.User)
                    .Where(p => p.ConversationId == id)
                    .ToListAsync();

                foreach (var p in participants)
                {
                    var lastSeen = await _presenceService.GetLastSeenAsync(p.UserId);
                    model.Participants.Add(new ConversationParticipantViewModel
                    {
                        UserId = p.UserId,
                        FullName = $"{p.User.FirstName} {p.User.LastName}".Trim(),
                        Email = p.User.Email,
                        UserType = p.User.UserType,
                        IsOnline = _presenceService.IsOnline(p.UserId),
                        LastSeen = lastSeen,
                        JoinedDate = p.JoinedDate,
                        LastReadDate = p.LastReadDate
                    });
                }

                foreach (var m in messages.OrderBy(m => m.CreatedDate))
                {
                    model.Messages.Add(new MessageViewModel
                    {
                        Id = m.Id,
                        ConversationId = m.ConversationId,
                        SenderId = m.SenderId,
                        SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}".Trim() : "Unknown",
                        SenderAvatarInitials = m.Sender != null ? GetInitials(m.Sender.FirstName, m.Sender.LastName) : "?",
                        SenderRole = m.Sender?.UserType,
                        MessageText = m.MessageText,
                        CreatedDate = m.CreatedDate,
                        IsRead = m.IsRead,
                        IsEdited = m.IsEdited,
                        EditedDate = m.EditedDate,
                        IsMine = m.SenderId == currentUser.Id,
                        HasAttachment = m.Attachment != null,
                        AttachmentFileName = m.Attachment?.FileName,
                        AttachmentUrl = m.Attachment != null ? Url.Action("Download", "Files", new { id = m.Attachment.Id }) : null
                    });
                }

                await _messageService.MarkMessagesAsReadAsync(id, currentUser.Id);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation {ConversationId}.", id);
                TempData["ErrorMessage"] = "An error occurred while loading the conversation.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send([FromBody] SendMessageViewModel model)
        {
            if (model == null) return BadRequest(new { success = false, error = "Invalid request." });
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                if (!await _messageService.HasAccessToConversationAsync(model.ConversationId, currentUser.Id))
                    return Forbid();

                var text = (model.MessageText ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(text))
                    return BadRequest(new { success = false, error = "Message cannot be empty." });
                if (text.Length > 5000)
                    return BadRequest(new { success = false, error = "Message exceeds 5000 characters." });

                var message = await _messageService.SendMessageAsync(model.ConversationId, currentUser.Id, text, model.AttachmentId);

                return Ok(new
                {
                    success = true,
                    message = new
                    {
                        id = message.Id,
                        conversationId = message.ConversationId,
                        senderId = currentUser.Id,
                        senderName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                        senderAvatarInitials = GetInitials(currentUser.FirstName, currentUser.LastName),
                        messageText = message.MessageText,
                        createdDate = message.CreatedDate,
                        isRead = false,
                        isMine = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message.");
                return StatusCode(500, new { success = false, error = "Server error sending message." });
            }
        }

        [HttpGet("History/{id:int}")]
        public async Task<IActionResult> History(int id, int page = 1, int pageSize = 50)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();
                if (!await _messageService.HasAccessToConversationAsync(id, currentUser.Id)) return Forbid();

                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 200);

                var messages = await _messageService.GetConversationMessagesAsync(id, page, pageSize);
                var result = messages.OrderBy(m => m.CreatedDate).Select(m => new
                {
                    id = m.Id,
                    conversationId = m.ConversationId,
                    senderId = m.SenderId,
                    senderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}".Trim() : "Unknown",
                    senderAvatarInitials = m.Sender != null ? GetInitials(m.Sender.FirstName, m.Sender.LastName) : "?",
                    senderRole = m.Sender?.UserType,
                    messageText = m.MessageText,
                    createdDate = m.CreatedDate,
                    isRead = m.IsRead,
                    isEdited = m.IsEdited,
                    isMine = m.SenderId == currentUser.Id
                });

                return Ok(new { success = true, messages = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading history for conversation {Id}.", id);
                return StatusCode(500, new { success = false, error = "Server error." });
            }
        }

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string? q)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();
                if (string.IsNullOrWhiteSpace(q))
                    return Ok(new { success = true, conversations = Array.Empty<object>(), messages = Array.Empty<object>() });

                var term = q.Trim().ToLowerInvariant();

                IQueryable<Conversation> accessibleQuery = _context.Conversations
                    .Include(c => c.Order).Where(c => !c.IsArchived);

                if (!User.IsInRole(RoleNames.Administrator))
                {
                    var myConvIds = await _context.ConversationParticipants
                        .Where(p => p.UserId == currentUser.Id).Select(p => p.ConversationId).ToListAsync();
                    accessibleQuery = accessibleQuery.Where(c => myConvIds.Contains(c.Id));
                }

                var conversations = await accessibleQuery.ToListAsync();
                var convIds = conversations.Select(c => c.Id).ToList();
                var participants = await _context.ConversationParticipants
                    .Include(p => p.User).Where(p => convIds.Contains(p.ConversationId)).ToListAsync();

                var matchingConvs = conversations.Where(c =>
                    (c.Order?.OrderNumber ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (c.Order?.Title ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    participants.Where(p => p.ConversationId == c.Id)
                        .Any(p => ($"{p.User.FirstName} {p.User.LastName}").ToLowerInvariant().Contains(term)))
                    .Take(25).Select(c => new
                    {
                        id = c.Id,
                        orderNumber = c.Order?.OrderNumber,
                        orderTitle = c.Order?.Title,
                        participants = participants.Where(p => p.ConversationId == c.Id)
                            .Select(p => $"{p.User.FirstName} {p.User.LastName}".Trim()).ToList(),
                        lastMessageDate = c.LastMessageDate
                    });

                var myConvIdList = await _context.ConversationParticipants
                    .Where(p => p.UserId == currentUser.Id).Select(p => p.ConversationId).ToListAsync();

                IQueryable<Message> messagesQuery = _context.Messages
                    .Include(m => m.Sender).Include(m => m.Conversation).ThenInclude(c => c!.Order)
                    .Where(m => m.MessageText.ToLower().Contains(term));

                if (!User.IsInRole(RoleNames.Administrator))
                    messagesQuery = messagesQuery.Where(m => myConvIdList.Contains(m.ConversationId));

                var matchingMessages = await messagesQuery.OrderByDescending(m => m.CreatedDate).Take(50)
                    .Select(m => new
                    {
                        id = m.Id,
                        conversationId = m.ConversationId,
                        orderNumber = m.Conversation!.Order!.OrderNumber,
                        senderName = m.Sender!.FirstName + " " + m.Sender.LastName,
                        messageText = m.MessageText,
                        createdDate = m.CreatedDate
                    }).ToListAsync();

                return Ok(new { success = true, conversations = matchingConvs, messages = matchingMessages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing message search.");
                return StatusCode(500, new { success = false, error = "Server error." });
            }
        }

        [HttpGet("UnreadCount")]
        public async Task<IActionResult> UnreadCount()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();
                var count = await _messageService.GetUnreadMessageCountAsync(currentUser.Id);
                return Ok(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count.");
                return Ok(new { success = false, count = 0 });
            }
        }

        [HttpGet("ForOrder/{orderId:int}")]
        public async Task<IActionResult> ForOrder(int orderId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null) return NotFound();

                if (!User.IsInRole(RoleNames.Administrator) && order.ClientId != currentUser.Id && order.AssignedWriterId != currentUser.Id)
                    return Forbid();

                var conv = await _messageService.GetOrCreateConversationAsync(orderId);
                return RedirectToAction(nameof(Conversation), new { id = conv.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening conversation for order {OrderId}.", orderId);
                TempData["ErrorMessage"] = "An error occurred while opening the conversation.";
                return RedirectToAction("Index", "Orders");
            }
        }

        [HttpGet("Recent")]
        public async Task<IActionResult> Recent(int take = 5)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                take = Math.Clamp(take, 1, 25);
                var conversations = await _messageService.GetUserConversationsAsync(currentUser.Id);

                if (User.IsInRole(RoleNames.Administrator))
                {
                    var allConvs = await _context.Conversations.Include(c => c.Order)
                        .Where(c => !c.IsArchived).OrderByDescending(c => c.LastMessageDate).ToListAsync();
                    var existingIds = conversations.Select(c => c.Id).ToHashSet();
                    foreach (var c in allConvs.Where(c => !existingIds.Contains(c.Id)))
                        conversations.Add(c);
                    conversations = conversations.OrderByDescending(c => c.LastMessageDate).Take(take).ToList();
                }
                else
                {
                    conversations = conversations.Take(take).ToList();
                }

                var result = new List<object>();
                foreach (var c in conversations)
                {
                    var lastMessage = await _context.Messages.Where(m => m.ConversationId == c.Id)
                        .OrderByDescending(m => m.CreatedDate).FirstOrDefaultAsync();
                    var participants = await _context.ConversationParticipants.Include(p => p.User)
                        .Where(p => p.ConversationId == c.Id).ToListAsync();
                    var otherParty = participants.FirstOrDefault(p => p.UserId != currentUser.Id)?.User;
                    if (otherParty == null && !User.IsInRole(RoleNames.Administrator)) continue;

                    var unread = await _context.Messages.Where(m => m.ConversationId == c.Id && m.SenderId != currentUser.Id && !m.IsRead).CountAsync();
                    var displayName = otherParty != null ? $"{otherParty.FirstName} {otherParty.LastName}".Trim() : "Conversation";

                    result.Add(new
                    {
                        conversationId = c.Id,
                        orderNumber = c.Order?.OrderNumber,
                        orderTitle = c.Order?.Title,
                        otherPartyName = displayName,
                        lastMessagePreview = lastMessage != null && lastMessage.MessageText.Length > 80
                            ? lastMessage.MessageText.Substring(0, 80) + "..." : lastMessage?.MessageText,
                        lastMessageDate = lastMessage?.CreatedDate ?? c.LastMessageDate,
                        unreadCount = unread,
                        isOtherPartyOnline = otherParty != null && _presenceService.IsOnline(otherParty.Id)
                    });
                }

                return Ok(new { success = true, conversations = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent conversations.");
                return Ok(new { success = false, conversations = Array.Empty<object>() });
            }
        }

        [HttpPost("MarkRead/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();
                if (!await _messageService.HasAccessToConversationAsync(id, currentUser.Id)) return Forbid();
                await _messageService.MarkMessagesAsReadAsync(id, currentUser.Id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking conversation {Id} as read.", id);
                return StatusCode(500, new { success = false });
            }
        }

        private async Task<ConversationListViewModel?> BuildConversationListItemAsync(Conversation conv, ApplicationUser currentUser)
        {
            if (!User.IsInRole(RoleNames.Administrator))
            {
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(p => p.ConversationId == conv.Id && p.UserId == currentUser.Id);
                if (!isParticipant) return null;
            }

            var lastMessage = await _context.Messages.Where(m => m.ConversationId == conv.Id)
                .OrderByDescending(m => m.CreatedDate).FirstOrDefaultAsync();
            var unread = await _context.Messages
                .Where(m => m.ConversationId == conv.Id && m.SenderId != currentUser.Id && !m.IsRead).CountAsync();
            var participants = await _context.ConversationParticipants.Include(p => p.User)
                .Where(p => p.ConversationId == conv.Id).ToListAsync();

            var item = new ConversationListViewModel
            {
                Id = conv.Id,
                OrderId = conv.OrderId,
                OrderNumber = conv.Order?.OrderNumber ?? string.Empty,
                OrderTitle = conv.Order?.Title ?? string.Empty,
                LastMessageDate = conv.LastMessageDate,
                CreatedDate = conv.CreatedDate,
                UnreadCount = unread,
                LastMessagePreview = lastMessage != null && lastMessage.MessageText.Length > 100
                    ? lastMessage.MessageText.Substring(0, 100) + "..." : lastMessage?.MessageText
            };

            foreach (var p in participants)
            {
                item.Participants.Add(new ConversationParticipantViewModel
                {
                    UserId = p.UserId,
                    FullName = $"{p.User.FirstName} {p.User.LastName}".Trim(),
                    Email = p.User.Email,
                    UserType = p.User.UserType,
                    IsOnline = _presenceService.IsOnline(p.UserId),
                    LastSeen = await _presenceService.GetLastSeenAsync(p.UserId),
                    JoinedDate = p.JoinedDate,
                    LastReadDate = p.LastReadDate
                });
            }

            var otherParty = participants.FirstOrDefault(p => p.UserId != currentUser.Id)?.User;
            if (otherParty != null)
            {
                item.OtherPartyName = $"{otherParty.FirstName} {otherParty.LastName}".Trim();
                item.OtherPartyId = otherParty.Id;
                item.IsOtherPartyOnline = _presenceService.IsOnline(otherParty.Id);
                item.OtherPartyLastSeen = await _presenceService.GetLastSeenAsync(otherParty.Id);
            }
            else
            {
                item.OtherPartyName = string.Join(", ", participants.Select(p => $"{p.User.FirstName} {p.User.LastName}".Trim()));
            }

            return item;
        }

        private static string GetInitials(string firstName, string lastName)
        {
            var first = !string.IsNullOrWhiteSpace(firstName) ? firstName.Trim()[0].ToString().ToUpperInvariant() : string.Empty;
            var last = !string.IsNullOrWhiteSpace(lastName) ? lastName.Trim()[0].ToString().ToUpperInvariant() : string.Empty;
            return (first + last).Trim();
        }
    }
}
