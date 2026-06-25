using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Services;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Progressive delivery milestones. Admin creates, writer submits files,
    /// client approves. All participants see the shared timeline.
    /// </summary>
    [Authorize]
    public class OrderMilestonesController : Controller
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".zip" };
        private const long MaxFileSize = 25L * 1024 * 1024;

        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderMilestoneService _milestoneService;
        private readonly IWriterApplicationService _writerApplicationService;
        private readonly ILogger<OrderMilestonesController> _logger;

        public OrderMilestonesController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            IOrderMilestoneService milestoneService,
            IWriterApplicationService writerApplicationService,
            ILogger<OrderMilestonesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _milestoneService = milestoneService;
            _writerApplicationService = writerApplicationService;
            _logger = logger;
        }

        /// <summary>
        /// Shared timeline visible to all participants (client, writer, admin).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Timeline(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.AssignedWriter)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            bool isAdmin = User.IsInRole("Administrator");
            bool isClient = order.ClientId == currentUser.Id;
            bool isWriter = order.AssignedWriterId == currentUser.Id;

            if (!isAdmin && !isClient && !isWriter)
                return Forbid();

            var timeline = await _milestoneService.GetTimelineAsync(orderId);
            var requiredRule = _milestoneService.IsProgressiveDeliveryRequired(order.Pages)
                ? "Mandatory"
                : _milestoneService.IsProgressiveDeliveryOptional(order.Pages)
                    ? "Optional"
                    : "Not used";

            ViewBag.Order = order;
            ViewBag.PageRule = requiredRule;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsClient = isClient;
            ViewBag.IsWriter = isWriter;

            return View(timeline);
        }

        // ──────────────────────────────────────────────
        // Admin: create / edit / delete milestones
        // ──────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            var nextSort = await _milestoneService.GetNextSortOrderAsync(orderId);
            var model = new OrderMilestone
            {
                OrderId = orderId,
                SortOrder = nextSort,
                Deadline = order.Deadline,
                Status = MilestoneStatus.Pending
            };
            ViewBag.Order = order;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(OrderMilestone model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Order = await _context.Orders.FindAsync(model.OrderId);
                return View(model);
            }

            try
            {
                await _milestoneService.CreateMilestoneAsync(model);
                TempData["SuccessMessage"] = $"Milestone \"{model.Title}\" created.";
                return RedirectToAction(nameof(Timeline), new { orderId = model.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone for order {OrderId}.", model.OrderId);
                ModelState.AddModelError("", ex.Message);
                ViewBag.Order = await _context.Orders.FindAsync(model.OrderId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int id)
        {
            var milestone = await _milestoneService.GetByIdAsync(id);
            if (milestone == null) return NotFound();

            try
            {
                await _milestoneService.DeleteMilestoneAsync(id);
                TempData["SuccessMessage"] = "Milestone deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting milestone {Id}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Timeline), new { orderId = milestone.OrderId });
        }

        // ──────────────────────────────────────────────
        // Writer: submit files for a milestone
        // ──────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Writer,Administrator")]
        public async Task<IActionResult> Submit(int id)
        {
            var milestone = await _milestoneService.GetByIdAsync(id);
            if (milestone == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var order = await _context.Orders.FindAsync(milestone.OrderId);

            if (currentUser == null) return Challenge();
            bool isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && order?.AssignedWriterId != currentUser.Id)
                return Forbid();

            if (milestone.Status == MilestoneStatus.Approved)
            {
                TempData["ErrorMessage"] = "This milestone has already been approved and cannot be edited.";
                return RedirectToAction(nameof(Timeline), new { orderId = milestone.OrderId });
            }

            var files = await _milestoneService.GetFilesAsync(id);
            ViewBag.Milestone = milestone;
            ViewBag.Order = order;
            ViewBag.Files = files;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Writer,Administrator")]
        public async Task<IActionResult> Submit(int id, string? notes, List<IFormFile>? files)
        {
            var milestone = await _milestoneService.GetByIdAsync(id);
            if (milestone == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                if (files == null || files.Count == 0)
                    throw new InvalidOperationException("Please upload at least one file.");

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "milestones", milestone.Id.ToString());
                Directory.CreateDirectory(uploadsDir);

                var uploaded = new List<(string FileName, string FilePath, long FileSize, string? Description)>();
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(ext))
                        throw new InvalidOperationException($"Invalid file type: {file.FileName}");
                    if (file.Length > MaxFileSize)
                        throw new InvalidOperationException($"File too large (max 25MB): {file.FileName}");

                    var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{file.FileName}";
                    var filePath = Path.Combine(uploadsDir, safeName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    uploaded.Add((
                        file.FileName,
                        $"/uploads/milestones/{milestone.Id}/{safeName}",
                        file.Length,
                        null
                    ));
                }

                await _milestoneService.SubmitMilestoneAsync(id, currentUser.Id, uploaded, notes);
                TempData["SuccessMessage"] = "Milestone files submitted. The client will review and approve.";
                return RedirectToAction(nameof(Timeline), new { orderId = milestone.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting milestone {Id}.", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Submit), new { id });
            }
        }

        // ──────────────────────────────────────────────
        // Client: approve a submitted milestone
        // ──────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Administrator")]
        public async Task<IActionResult> Approve(int id, string? notes)
        {
            var milestone = await _milestoneService.GetByIdAsync(id);
            if (milestone == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                await _milestoneService.ApproveMilestoneAsync(id, currentUser.Id, notes);
                TempData["SuccessMessage"] = "Milestone approved and writer earnings recorded.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving milestone {Id}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Timeline), new { orderId = milestone.OrderId });
        }
    }
}
