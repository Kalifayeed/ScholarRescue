using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Services;

namespace ScholarRescue.Controllers
{
    [Authorize]
    public class SupportTicketsController : Controller
    {
        private readonly ISupportTicketService _ticketService;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly string[] AllowedExts = { ".pdf", ".doc", ".docx", ".zip", ".jpg", ".png" };
        private const long MaxSize = 25L * 1024 * 1024;

        public SupportTicketsController(ISupportTicketService ticketService, UserManager<ApplicationUser> userManager)
        {
            _ticketService = ticketService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(SupportDepartment? department = null, TicketStatus? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            bool isAdmin = User.IsInRole("Administrator");
            var tickets = await _ticketService.GetTicketsAsync(isAdmin ? null : user.Id, department, status);
            ViewBag.DepartmentFilter = department;
            ViewBag.StatusFilter = status;
            ViewBag.IsAdmin = isAdmin;
            return View(tickets);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string subject, string description, SupportDepartment department, int? orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            try
            {
                var ticket = await _ticketService.CreateTicketAsync(subject, description, department, user.Id, orderId);
                TempData["SuccessMessage"] = $"Ticket #{ticket.TicketNumber} created.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _ticketService.GetByIdAsync(id);
            if (ticket == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            bool isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && ticket.CreatorId != user.Id) return Forbid();
            var notes = await _ticketService.GetNotesAsync(id);
            var attachments = await _ticketService.GetAttachmentsAsync(id);
            ViewBag.Notes = notes;
            ViewBag.Attachments = attachments;
            ViewBag.IsAdmin = isAdmin;
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ChangeStatus(int id, TicketStatus newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            try
            {
                await _ticketService.ChangeStatusAsync(id, newStatus, user.Id);
                TempData["SuccessMessage"] = $"Status updated to {newStatus}.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int id, string content, bool isInternal)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            try
            {
                await _ticketService.AddNoteAsync(id, content, user.Id, isInternal);
                TempData["SuccessMessage"] = "Note added.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            try
            {
                if (file == null || file.Length == 0) throw new InvalidOperationException("No file selected.");
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExts.Contains(ext)) throw new InvalidOperationException("Invalid file type.");
                if (file.Length > MaxSize) throw new InvalidOperationException("File too large (max 25MB).");

                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "support-tickets", id.ToString());
                Directory.CreateDirectory(dir);
                var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{file.FileName}";
                var path = Path.Combine(dir, safeName);
                using (var s = new FileStream(path, FileMode.Create)) { await file.CopyToAsync(s); }

                await _ticketService.AddAttachmentAsync(id, file.FileName, $"/uploads/support-tickets/{id}/{safeName}", file.Length, user.Id);
                TempData["SuccessMessage"] = "File uploaded.";
            }
            catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}