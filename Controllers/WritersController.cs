using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Services;
using ScholarRescue.ViewModels.Order;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller for writer-specific functionality: viewing assigned orders,
    /// updating order status, uploading completed work, internal notes, financial management,
    /// the Available Orders marketplace, and the writer application status workflow.
    /// </summary>
    [Authorize(Roles = "Writer,Administrator")]
    public class WritersController : Controller
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFinancialService _financialService;
        private readonly IPayoutWindowService _payoutWindowService;
        private readonly IWriterApplicationService _writerApplicationService;
        private readonly IOrderAssignmentService _orderAssignmentService;
        private readonly IWriterRankingService _rankingService;
        private readonly ILogger<WritersController> _logger;

        public WritersController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            IFinancialService financialService,
            IPayoutWindowService payoutWindowService,
            IWriterApplicationService writerApplicationService,
            IOrderAssignmentService orderAssignmentService,
            IWriterRankingService rankingService,
            ILogger<WritersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _financialService = financialService;
            _payoutWindowService = payoutWindowService;
            _writerApplicationService = writerApplicationService;
            _orderAssignmentService = orderAssignmentService;
            _rankingService = rankingService;
            _logger = logger;
        }

        /// <summary>
        /// Writer Performance Analytics dashboard.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Writer,Administrator")]
        public async Task<IActionResult> Analytics()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                return RedirectToAction("Dashboard");
            }

            var model = await _rankingService.GetAnalyticsAsync(currentUser.Id);
            return View(model);
        }

        /// <summary>
        /// Writer updates their availability status (Available, Busy, HighValueOnly, Unavailable).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Writer,Administrator")]
        public async Task<IActionResult> UpdateAvailability(WriterAvailabilityStatus status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                currentUser.AvailabilityStatus = status;
                currentUser.LastActivityDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(currentUser);

                _logger.LogInformation("Writer {WriterId} set availability to {Status}.", currentUser.Id, status);
                TempData["SuccessMessage"] = $"Availability updated to {status}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating availability for writer {WriterId}.", currentUser.Id);
                TempData["ErrorMessage"] = "Error updating availability.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        /// <summary>
        /// Writer dashboard.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                // Admin users see writer's view for testing
                bool isAdmin = User.IsInRole("Administrator");

                var access = await _writerApplicationService.GetAccessStateAsync(currentUser.Id);

                // Surface the access state to the view
                ViewBag.AccessState = access;
                ViewBag.WriterApplication = await _writerApplicationService.GetLatestApplicationAsync(currentUser.Id);

                if (!access.HasApplication && !isAdmin)
                {
                    return View("ApplicationRequired");
                }

                if (access.Status == WriterApplicationStatus.Pending)
                {
                    return View("Pending");
                }

                if (access.Status == WriterApplicationStatus.Rejected)
                {
                    return View("Rejected");
                }

                if (access.IsSuspended)
                {
                    return View("Suspended");
                }

                // Approved writer: show full dashboard
                var assignedOrders = _context.Orders
                    .Include(o => o.Client)
                    .Where(o => o.AssignedWriterId == currentUser.Id)
                    .AsNoTracking();

                var totalAssigned = await assignedOrders.CountAsync();
                var inProgress = await assignedOrders.CountAsync(o =>
                    o.Status == OrderStatus.InProgress || o.Status == OrderStatus.Assigned);
                var completed = await assignedOrders.CountAsync(o => o.Status == OrderStatus.Completed);

                var pendingAssignment = await _context.Orders
                    .CountAsync(o => o.Status == OrderStatus.Open
                        && o.AssignedWriterId == null
                        && o.IsMarketplaceOpen
                        && o.Deadline > DateTime.UtcNow);

                var recentOrders = await assignedOrders
                    .OrderByDescending(o => o.UpdatedAt)
                    .Take(10)
                    .Select(o => new OrderIndexViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Title = o.Title,
                        ClientName = o.Client.FirstName + " " + o.Client.LastName,
                        Status = o.Status,
                        Deadline = o.Deadline,
                        Pages = o.Pages,
                        Budget = o.Budget,
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync();

                var myApplications = await _orderAssignmentService.GetApplicationsByWriterAsync(currentUser.Id);
                ViewBag.MyApplications = myApplications.Take(5).ToList();
                ViewBag.MyApplicationsCount = myApplications.Count;

                ViewBag.TotalAssigned = totalAssigned;
                ViewBag.InProgress = inProgress;
                ViewBag.Completed = completed;
                ViewBag.PendingAssignment = pendingAssignment;

                // Available orders for the writer dashboard marketplace section
                var availableOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Open
                        && o.AssignedWriterId == null
                        && o.IsMarketplaceOpen
                        && o.Deadline > DateTime.UtcNow)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new OrderIndexViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Title = o.Title,
                        Status = o.Status,
                        Deadline = o.Deadline,
                        Pages = o.Pages,
                        Budget = o.Budget,
                        CreatedAt = o.CreatedAt,
                        Subject = o.Subject,
                        AcademicLevel = o.AcademicLevel,
                        CitationFormat = o.CitationFormat
                    })
                    .ToListAsync();
                ViewBag.AvailableOrders = availableOrders;

                // Load writer's bids
                var myBids = await _context.OrderBids
                    .Where(b => b.WriterId == currentUser.Id)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new ViewModels.Writer.WriterBidViewModel
                    {
                        Id = b.Id,
                        OrderId = b.OrderId,
                        OrderNumber = b.Order.OrderNumber,
                        OrderTitle = b.Order.Title,
                        Amount = b.Amount,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();
                ViewBag.MyBids = myBids;
                ViewBag.MyBidsCount = myBids.Count;

                // Load writer ranking data for badge display
                var ranking = await _rankingService.GetOrCreateAsync(currentUser.Id);
                ViewBag.WriterRank = ranking.CurrentRank;
                ViewBag.WriterRanking = ranking;
                ViewBag.WriterAvailability = currentUser.AvailabilityStatus;

                return View(recentOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer dashboard.");
                TempData["ErrorMessage"] = "Error loading dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Shows the list of orders available in the marketplace (approved writers only).
        /// Supports advanced filtering and sorting.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AvailableOrders(
            string? discipline = null,
            AcademicLevel? academicLevel = null,
            int? minPages = null,
            int? maxPages = null,
            DateTime? deadlineFrom = null,
            DateTime? deadlineTo = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            CitationFormat? citationStyle = null,
            PriorityLevel? urgency = null,
            string sortBy = "newest")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] =
                    "Your writer application must be approved before you can browse available orders.";
                return RedirectToAction("Dashboard");
            }

            var orders = await _orderAssignmentService.GetAvailableOrdersFilteredAsync(
                discipline, academicLevel, minPages, maxPages,
                deadlineFrom, deadlineTo, minPrice, maxPrice,
                citationStyle, urgency, sortBy);

            var myApplications = await _orderAssignmentService.GetApplicationsByWriterAsync(currentUser.Id);
            var myAppliedOrderIds = myApplications
                .Where(a => a.Status == OrderApplicationStatus.Pending
                    || a.Status == OrderApplicationStatus.Selected)
                .Select(a => a.OrderId)
                .ToHashSet();

            ViewBag.MyAppliedOrderIds = myAppliedOrderIds;
            ViewBag.CurrentSortBy = sortBy;
            ViewBag.Discipline = discipline;
            ViewBag.AcademicLevel = academicLevel;
            ViewBag.MinPages = minPages;
            ViewBag.MaxPages = maxPages;
            ViewBag.DeadlineFrom = deadlineFrom;
            ViewBag.DeadlineTo = deadlineTo;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CitationStyle = citationStyle;
            ViewBag.Urgency = urgency;

            return View(orders);
        }

        /// <summary>
        /// Lets a writer view the full details of an order in the marketplace.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Documents)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Authorization: writer must be approved and order must be in marketplace,
            // OR the writer is already the assigned writer, OR admin.
            bool isApprovedWriter = await _writerApplicationService.IsWriterActiveAsync(currentUser.Id);
            bool isAssignedWriter = order.AssignedWriterId == currentUser.Id;
            bool isAdmin = User.IsInRole("Administrator");

            if (!isAdmin && !isAssignedWriter &&
                (!isApprovedWriter || order.Status != OrderStatus.Open || !order.IsMarketplaceOpen))
            {
                return Forbid();
            }

            var myApplication = await _context.OrderApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.OrderId == id && a.WriterId == currentUser.Id);

            ViewBag.MyApplication = myApplication;
            ViewBag.IsAssignedWriter = isAssignedWriter;
            ViewBag.IsAdmin = isAdmin;

            return View(order);
        }

        /// <summary>
        /// Allows an approved writer to apply to an open order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyForOrder(int id, string? message)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] =
                    "Your writer application must be approved before you can apply to orders.";
                return RedirectToAction("AvailableOrders");
            }

            try
            {
                await _orderAssignmentService.ApplyForOrderAsync(id, currentUser.Id, message);
                TempData["SuccessMessage"] = "Your application has been submitted. The admin team will review it shortly.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying to order {Id} for writer {WriterId}.", id, currentUser.Id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("AvailableOrders");
        }

        // ════════════════════════════════════════════════
        // ORDER BIDDING (Phase 2)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Shows the bid form for a writer to place a bid on an available order.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Writer,Administrator")]
        public async Task<IActionResult> PlaceBid(int orderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] = "Your writer application must be approved before you can place bids.";
                return RedirectToAction("Dashboard");
            }

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            // Only allow bidding on open/marketplace orders
            if (order.Status != OrderStatus.Open || !order.IsMarketplaceOpen || order.Deadline <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "This order is no longer accepting bids.";
                return RedirectToAction("AvailableOrders");
            }

            // Check for existing active bid
            var existingBid = await _context.OrderBids
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.OrderId == orderId && b.WriterId == currentUser.Id
                    && (b.Status == OrderBidStatus.Pending || b.Status == OrderBidStatus.Accepted));

            if (existingBid != null)
            {
                TempData["ErrorMessage"] = "You already have an active bid on this order.";
                return RedirectToAction("AvailableOrders");
            }

            var model = new ViewModels.Writer.PlaceBidViewModel
            {
                OrderId = order.Id,
                OrderTitle = order.Title,
                OrderNumber = order.OrderNumber,
                Amount = order.Budget, // Pre-fill with order budget as suggestion
                EstimatedDeliveryDate = order.Deadline > DateTime.UtcNow ? order.Deadline.AddDays(-1) : null
            };

            return View(model);
        }

        /// <summary>
        /// Processes the writer's bid submission.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Writer,Administrator")]
        public async Task<IActionResult> PlaceBid(ViewModels.Writer.PlaceBidViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!ModelState.IsValid)
            {
                // Re-populate order info
                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == viewModel.OrderId);
                if (order != null)
                {
                    viewModel.OrderTitle = order.Title;
                    viewModel.OrderNumber = order.OrderNumber;
                }
                return View(viewModel);
            }

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == viewModel.OrderId);

                if (order == null) return NotFound();

                if (order.Status != OrderStatus.Open || !order.IsMarketplaceOpen)
                {
                    TempData["ErrorMessage"] = "This order is no longer accepting bids.";
                    return RedirectToAction("AvailableOrders");
                }

                // Prevent duplicate active bids
                var existingBid = await _context.OrderBids
                    .FirstOrDefaultAsync(b => b.OrderId == viewModel.OrderId && b.WriterId == currentUser.Id
                        && (b.Status == OrderBidStatus.Pending || b.Status == OrderBidStatus.Accepted));

                if (existingBid != null)
                {
                    TempData["ErrorMessage"] = "You already have an active bid on this order.";
                    return RedirectToAction("AvailableOrders");
                }

                var bid = new OrderBid
                {
                    OrderId = viewModel.OrderId,
                    WriterId = currentUser.Id,
                    Amount = viewModel.Amount,
                    Message = viewModel.Message,
                    EstimatedDeliveryDate = viewModel.EstimatedDeliveryDate,
                    Status = OrderBidStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderBids.Add(bid);

                // Add audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Bid Placed",
                    PerformedById = currentUser.Id,
                    TargetUserId = order.ClientId,
                    Description = $"Writer placed a bid of ${viewModel.Amount:N2} on order {order.OrderNumber}.",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Writer {WriterId} placed bid of {Amount} on order {OrderId}.",
                    currentUser.Id, viewModel.Amount, viewModel.OrderId);

                TempData["SuccessMessage"] = $"Your bid of ${viewModel.Amount:N2} has been submitted successfully!";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing bid for writer {WriterId} on order {OrderId}.",
                    currentUser.Id, viewModel.OrderId);
                TempData["ErrorMessage"] = "An error occurred while placing your bid. Please try again.";
                return View(viewModel);
            }
        }

        /// <summary>
        /// Withdraw a pending order application.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawApplication(int applicationId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                await _orderAssignmentService.WithdrawApplicationAsync(applicationId, currentUser.Id);
                TempData["SuccessMessage"] = "Application withdrawn successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application {AppId} for writer {WriterId}.",
                    applicationId, currentUser.Id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Dashboard");
        }

        /// <summary>
        /// Allows writer to update the status of an assigned order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders
                    .Include(o => o.Client)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                if (order.AssignedWriterId != currentUser.Id && !User.IsInRole("Administrator"))
                    return Forbid();

                // Validate status transitions for writers
                var validTransitions = new[]
                {
                    OrderStatus.InProgress,
                    OrderStatus.DraftSubmitted
                };

                if (!validTransitions.Contains(newStatus))
                {
                    TempData["ErrorMessage"] = "Invalid status transition.";
                    return RedirectToAction("Dashboard");
                }

                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {Id} status updated to {Status}.", id, newStatus);
                TempData["SuccessMessage"] = $"Order {order.OrderNumber} status updated to {newStatus}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {Id}.", id);
                TempData["ErrorMessage"] = "Error updating order status.";
            }

            return RedirectToAction("Dashboard");
        }

        // ──────────────────────────────────────────────
        // Financial Management
        // ──────────────────────────────────────────────

        /// <summary>
        /// Writer wallet dashboard showing balances and recent transactions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Wallet()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!await _writerApplicationService.IsWriterActiveAsync(currentUser.Id) &&
                    !User.IsInRole("Administrator"))
                {
                    TempData["ErrorMessage"] =
                        "Your writer application must be approved before you can access earnings.";
                    return RedirectToAction("Dashboard");
                }

                var dashboard = await _financialService.GetWriterDashboardAsync(currentUser.Id);
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer wallet.");
                TempData["ErrorMessage"] = "Error loading wallet.";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Writer transaction history.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Transactions(int page = 1)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!await _writerApplicationService.IsWriterActiveAsync(currentUser.Id) &&
                    !User.IsInRole("Administrator"))
                {
                    return RedirectToAction("Dashboard");
                }

                var transactions = await _financialService.GetUserTransactionsAsync(currentUser.Id, page, 20);
                ViewBag.CurrentPage = page;
                return View(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer transactions.");
                TempData["ErrorMessage"] = "Error loading transactions.";
                return RedirectToAction("Wallet");
            }
        }

        /// <summary>
        /// Writer payout requests page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Payouts()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!await _writerApplicationService.IsWriterActiveAsync(currentUser.Id) &&
                    !User.IsInRole("Administrator"))
                {
                    return RedirectToAction("Dashboard");
                }

                var payouts = await _financialService.GetWriterPayoutsAsync(currentUser.Id);
                ViewBag.PayoutWindowOpen = _payoutWindowService.IsPayoutWindowOpen();
                ViewBag.PayoutWindowMessage = _payoutWindowService.GetPayoutWindowMessage();
                ViewBag.TimeUntilNextWindow = _payoutWindowService.GetTimeUntilNextWindow();
                return View(payouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer payouts.");
                TempData["ErrorMessage"] = "Error loading payout requests.";
                return RedirectToAction("Wallet");
            }
        }

        /// <summary>
        /// Request a payout from available balance.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPayout(decimal amount)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!await _writerApplicationService.IsWriterActiveAsync(currentUser.Id) &&
                    !User.IsInRole("Administrator"))
                {
                    return RedirectToAction("Dashboard");
                }

                if (!_payoutWindowService.IsPayoutWindowOpen())
                {
                    TempData["ErrorMessage"] = "Payout requests are currently closed. Windows are open on the 1st and 15th of each month.";
                    return RedirectToAction("Payouts");
                }

                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "Amount must be greater than zero.";
                    return RedirectToAction("Payouts");
                }

                var payout = await _financialService.ProcessPayoutRequestAsync(currentUser.Id, amount);
                TempData["SuccessMessage"] = $"Payout request for ${amount:N2} submitted successfully. Transaction: {payout.TransactionNumber}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting payout.");
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction("Payouts");
        }

        /// <summary>
        /// Writer payment details management.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PaymentDetails()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var details = await _financialService.GetPaymentDetailsAsync(currentUser.Id);
                return View(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment details.");
                TempData["ErrorMessage"] = "Error loading payment details.";
                return RedirectToAction("Wallet");
            }
        }

        /// <summary>
        /// Save writer payment details.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentDetails(string paymentMethod, string? accountName,
            string? accountNumber, string? phoneNumber, string? bankName, string? payPalEmail)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                await _financialService.SavePaymentDetailsAsync(currentUser.Id, paymentMethod, accountName,
                    accountNumber, phoneNumber, bankName, payPalEmail);

                TempData["SuccessMessage"] = "Payment details saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving payment details.");
                TempData["ErrorMessage"] = "Error saving payment details.";
            }

            return RedirectToAction("PaymentDetails");
        }
    }
}
