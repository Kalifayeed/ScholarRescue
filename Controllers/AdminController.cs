using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;
using ScholarRescue.Services;
using ScholarRescue.Services.Matching;
using ScholarRescue.ViewModels.Admin;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller responsible for the admin dashboard and system administration.
    /// Provides user management, writer applications, audit logs, system oversight,
    /// and the order assignment workflow.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IOrderAssignmentService _orderAssignmentService;
        private readonly IWriterApplicationService _writerApplicationService;
        private readonly INotificationService _notificationService;
        private readonly IWriterResourceService _writerResourceService;
        private readonly IWriterRankingService _writerRankingService;
        private readonly IWorkDeliveryService _workDeliveryService;
        private readonly IWriterCapacityService _writerCapacityService;
        private readonly IWriterMatchingService _writerMatchingService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOrderAssignmentService orderAssignmentService,
            IWriterApplicationService writerApplicationService,
            INotificationService notificationService,
            IWriterResourceService writerResourceService,
            IWriterRankingService writerRankingService,
            IWorkDeliveryService workDeliveryService,
            IWriterCapacityService writerCapacityService,
            IWriterMatchingService writerMatchingService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _orderAssignmentService = orderAssignmentService;
            _writerApplicationService = writerApplicationService;
            _notificationService = notificationService;
            _writerResourceService = writerResourceService;
            _writerRankingService = writerRankingService;
            _workDeliveryService = workDeliveryService;
            _writerCapacityService = writerCapacityService;
            _writerMatchingService = writerMatchingService;
            _logger = logger;
        }

        /// <summary>
        /// Operations Center dashboard with real-time platform metrics.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var roles = await _userManager.GetRolesAsync(currentUser);
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                // ── Users ──
                var totalUsers = await _userManager.Users.CountAsync();
                var totalClients = (await _userManager.GetUsersInRoleAsync("Client")).Count;
                var totalWriters = (await _userManager.GetUsersInRoleAsync("Writer")).Count;
                var totalStaff = 0;
                var activeWriters = await _context.Users
                    .CountAsync(u => u.UserType == "Writer" && u.IsActive && u.IsAcceptingOrders);

                // ── Orders ──
                var totalOrders = await _context.Orders.CountAsync();
                var ordersToday = await _context.Orders.CountAsync(o => o.CreatedAt >= todayStart);
                var ordersThisMonth = await _context.Orders.CountAsync(o => o.CreatedAt >= monthStart);
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.PendingReview);
                var inProgressOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.InProgress || o.Status == OrderStatus.Assigned);
                var awaitingAssignment = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Open && o.AssignedWriterId == null);

                // ── Revenue ──
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .SumAsync(o => (decimal?)o.Budget) ?? 0;
                var revenueToday = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed && (o.CompletedAt ?? o.UpdatedAt) >= todayStart)
                    .SumAsync(o => (decimal?)o.Budget) ?? 0;
                var revenueThisMonth = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed && (o.CompletedAt ?? o.UpdatedAt) >= monthStart)
                    .SumAsync(o => (decimal?)o.Budget) ?? 0;
                var escrowBalance = await _context.Set<EscrowAccount>()
                    .Where(e => e.Status == EscrowStatus.Funded)
                    .SumAsync(e => (decimal?)e.FundedAmount) ?? 0;

                // ── Operations ──
                var pendingApplications = await _context.WriterApplications.CountAsync(a => a.Status == WriterApplicationStatus.Pending);
                var openDisputes = await _context.OrderDisputes.CountAsync(d => d.Status == "Open" || d.Status == "InReview");
                var pendingRevisions = await _context.RevisionRequests.CountAsync(r => r.Status == RevisionRequestStatus.Pending);
                var openFraudAlerts = await _context.Set<AccountFraudAlert>().CountAsync(a => a.Status == "Open");
                var pendingQa = await _context.Orders.CountAsync(o => o.Status == OrderStatus.PendingQA);

                // ── Support Tickets ──
                var openTickets = await _context.SupportTickets.CountAsync(t => t.Status == TicketStatus.Open);
                var pendingTickets = await _context.SupportTickets.CountAsync(t => t.Status == TicketStatus.PendingResponse || t.Status == TicketStatus.WaitingForUser);
                var inProgressTickets = await _context.SupportTickets.CountAsync(t => t.Status == TicketStatus.InProgress);
                var resolvedTickets = await _context.SupportTickets.CountAsync(t => t.Status == TicketStatus.Resolved);
                var totalTickets = await _context.SupportTickets.CountAsync();

                var dashboard = new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalOrders = totalOrders,
                    CurrentUserRole = roles.FirstOrDefault() ?? "None",
                    TotalClients = totalClients,
                    TotalStaff = totalStaff,
                    TotalWriters = totalWriters,
                    ActiveWriters = activeWriters,
                    OrdersToday = ordersToday,
                    OrdersThisMonth = ordersThisMonth,
                    PendingOrders = pendingOrders,
                    InProgressOrders = inProgressOrders,
                    OrdersAwaitingAssignment = awaitingAssignment,
                    TotalRevenue = totalRevenue,
                    RevenueToday = revenueToday,
                    RevenueThisMonth = revenueThisMonth,
                    EscrowBalance = escrowBalance,
                    PendingApplications = pendingApplications,
                    OpenDisputes = openDisputes,
                    PendingRevisions = pendingRevisions,
                    OpenFraudAlerts = openFraudAlerts,
                    PendingQaOrders = pendingQa,
                    OpenTickets = openTickets,
                    PendingTickets = pendingTickets,
                    InProgressTickets = inProgressTickets,
                    ResolvedTickets = resolvedTickets,
                    TotalTickets = totalTickets
                };

                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard.");
                TempData["ErrorMessage"] = "Error loading dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ════════════════════════════════════════════════
        // BID REVIEW & ASSIGNMENT (Phase 3)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Admin reviews all bids submitted for an order.
        /// Shows real writer identities for admin vetting.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderBids(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            var bids = await _context.OrderBids
                .Include(b => b.Writer)
                .Where(b => b.OrderId == order.Id)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new ViewModels.Admin.AdminBidItemViewModel
                {
                    BidId = b.Id,
                    WriterId = b.WriterId,
                    WriterDisplayName = b.Writer.FirstName + " " + b.Writer.LastName,
                    WriterEmail = b.Writer.Email ?? string.Empty,
                    Amount = b.Amount,
                    Message = b.Message,
                    EstimatedDeliveryDate = b.EstimatedDeliveryDate,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            var viewModel = new ViewModels.Admin.OrderBidAdminViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                OrderTitle = order.Title,
                ClientName = order.Client.FirstName + " " + order.Client.LastName,
                ClientEmail = order.Client.Email ?? string.Empty,
                IsAssigned = order.AssignedWriterId != null,
                AssignedWriterName = null, // populated below if needed
                Bids = bids
            };

            if (order.AssignedWriterId != null)
            {
                var writer = await _userManager.FindByIdAsync(order.AssignedWriterId);
                if (writer != null)
                    viewModel.AssignedWriterName = writer.FirstName + " " + writer.LastName;
            }

            return View(viewModel);
        }

        /// <summary>
        /// Admin assigns a writer to an order based on a bid.
        /// Accepts the selected bid, rejects others, updates order status.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignWriterFromBid(int orderId, int bidId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return NotFound();

                var selectedBid = await _context.OrderBids
                    .Include(b => b.Writer)
                    .FirstOrDefaultAsync(b => b.Id == bidId && b.OrderId == orderId);

                if (selectedBid == null) return NotFound();

                if (selectedBid.Status != OrderBidStatus.Pending)
                {
                    TempData["ErrorMessage"] = "This bid is no longer pending and cannot be accepted.";
                    return RedirectToAction(nameof(OrderBids), new { orderId });
                }

                if (order.Status != OrderStatus.Open || order.AssignedWriterId != null)
                {
                    TempData["ErrorMessage"] = "This order is no longer available for assignment.";
                    return RedirectToAction(nameof(OrderBids), new { orderId });
                }

                // 1. Accept the selected bid
                selectedBid.Status = OrderBidStatus.Accepted;
                selectedBid.UpdatedAt = DateTime.UtcNow;

                // 2. Reject all other pending bids for this order
                var otherPendingBids = await _context.OrderBids
                    .Where(b => b.OrderId == orderId && b.Id != bidId && b.Status == OrderBidStatus.Pending)
                    .ToListAsync();

                foreach (var otherBid in otherPendingBids)
                {
                    otherBid.Status = OrderBidStatus.Rejected;
                    otherBid.UpdatedAt = DateTime.UtcNow;
                }

                // 3. Update the order
                order.AssignedWriterId = selectedBid.WriterId;
                order.AssignedAt = DateTime.UtcNow;
                order.AssignedByAdminId = currentUser.Id;
                order.IsMarketplaceOpen = false;
                order.Status = OrderStatus.Assigned;
                order.UpdatedAt = DateTime.UtcNow;

                // 4. Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Writer Assigned via Bid",
                    PerformedById = currentUser.Id,
                    TargetUserId = selectedBid.WriterId,
                    Description = $"Admin assigned writer {selectedBid.Writer.Email} (bid ${selectedBid.Amount:N2}) to order {order.OrderNumber}. Other pending bids rejected.",
                    CreatedDate = DateTime.UtcNow
                });

                // 5. Order history
                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    OldStatus = OrderStatus.Open,
                    NewStatus = OrderStatus.Assigned,
                    ChangedById = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    Notes = $"Writer assigned via bid acceptance. Writer: {selectedBid.Writer.Email}, Amount: ${selectedBid.Amount:N2}"
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin {AdminId} assigned writer {WriterId} to order {OrderId} via bid {BidId}.",
                    currentUser.Id, selectedBid.WriterId, orderId, bidId);

                TempData["SuccessMessage"] =
                    $"Writer '{selectedBid.Writer.FirstName} {selectedBid.Writer.LastName}' assigned successfully. Other pending bids rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning writer from bid for order {OrderId}.", orderId);
                TempData["ErrorMessage"] = "An error occurred while assigning the writer.";
            }

            return RedirectToAction(nameof(OrderBids), new { orderId });
        }

        /// <summary>
        /// Displays all users with their roles for management.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.FirstName)
                    .ToListAsync();

                var userVms = new List<UserManagementViewModel>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var isLocked = await _userManager.IsLockedOutAsync(user);

                    userVms.Add(new UserManagementViewModel
                    {
                        Id = user.Id,
                        FullName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email ?? string.Empty,
                        UserType = user.UserType,
                        Role = roles.FirstOrDefault() ?? "None",
                        EmailConfirmed = user.EmailConfirmed,
                        IsLockedOut = isLocked,
                        IsActive = user.IsActive,
                        IsDeleted = user.IsDeleted,
                        CreatedAt = user.CreatedDate,
                        LastLoginAt = user.LastLoginDate
                    });
                }

                return View(userVms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management.");
                TempData["ErrorMessage"] = "Error loading users.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Displays detailed information for a specific user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UserDetails(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            var ordersAsClient = await _context.Orders
                .Where(o => o.ClientId == id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            var ordersAsWriter = await _context.Orders
                .Where(o => o.AssignedWriterId == id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            var writerApplication = await _context.WriterApplications
                .FirstOrDefaultAsync(a => a.UserId == id);

            var vm = new UserDetailsViewModel
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                Role = roles.FirstOrDefault() ?? "None",
                EmailConfirmed = user.EmailConfirmed,
                IsLockedOut = isLocked,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                CreatedAt = user.CreatedDate,
                LastLoginAt = user.LastLoginDate,
                OrdersAsClient = ordersAsClient,
                OrdersAsWriter = ordersAsWriter,
                WriterApplication = writerApplication
            };

            return View(vm);
        }

        /// <summary>
        /// Suspends a user (sets IsActive = false).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                user.IsActive = false;
                await _userManager.UpdateAsync(user);

                if (await _userManager.IsInRoleAsync(user, "Writer"))
                {
                    var application = await _writerApplicationService.GetLatestApplicationAsync(user.Id);
                    if (application != null && application.Status == WriterApplicationStatus.Approved)
                    {
                        application.Status = WriterApplicationStatus.Suspended;
                        application.ReviewedAt = DateTime.UtcNow;
                        application.ReviewedByAdminId = currentUser!.Id;
                        application.AdminComments =
                            (application.AdminComments ?? string.Empty)
                            + $"\n[{DateTime.UtcNow:yyyy-MM-dd}] Account suspended by admin.";
                    }
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "User Suspended",
                    PerformedById = currentUser!.Id,
                    TargetUserId = user.Id,
                    Description = $"User {user.Email} suspended by administrator.",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} suspended by {AdminId}.", id, currentUser.Id);
                TempData["SuccessMessage"] = "User suspended successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}.", id);
                TempData["ErrorMessage"] = "Error suspending user.";
            }

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// Activates a user (sets IsActive = true).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                user.IsActive = true;
                await _userManager.UpdateAsync(user);

                if (await _userManager.IsInRoleAsync(user, "Writer"))
                {
                    var application = await _writerApplicationService.GetLatestApplicationAsync(user.Id);
                    if (application != null && application.Status == WriterApplicationStatus.Suspended)
                    {
                        application.Status = WriterApplicationStatus.Approved;
                        application.ReviewedAt = DateTime.UtcNow;
                        application.ReviewedByAdminId = currentUser!.Id;
                        application.AdminComments =
                            (application.AdminComments ?? string.Empty)
                            + $"\n[{DateTime.UtcNow:yyyy-MM-dd}] Account re-activated by admin.";
                    }
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "User Activated",
                    PerformedById = currentUser!.Id,
                    TargetUserId = user.Id,
                    Description = $"User {user.Email} activated by administrator.",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} activated by {AdminId}.", id, currentUser.Id);
                TempData["SuccessMessage"] = "User activated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}.", id);
                TempData["ErrorMessage"] = "Error activating user.";
            }

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// Soft deletes a user (sets IsDeleted = true).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            if (user.Id == currentUser!.Id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            try
            {
                user.IsDeleted = true;
                user.IsActive = false;
                await _userManager.UpdateAsync(user);

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "User Deleted (Soft)",
                    PerformedById = currentUser.Id,
                    TargetUserId = user.Id,
                    Description = $"User {user.Email} soft-deleted by administrator.",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} soft-deleted by {AdminId}.", id, currentUser.Id);
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}.", id);
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// Displays the assign role form for a specific user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AssignRole(string? id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

            ViewBag.UserId = user.Id;
            ViewBag.UserName = $"{user.FirstName} {user.LastName}";
            ViewBag.CurrentRole = currentRoles.FirstOrDefault() ?? "None";
            ViewBag.AllRoles = allRoles;

            return View();
        }

        /// <summary>
        /// Handles role assignment for a user.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                if (!string.IsNullOrEmpty(role) && role != "None")
                {
                    await _userManager.AddToRoleAsync(user, role);
                    user.UserType = role;
                    await _userManager.UpdateAsync(user);
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Role Assigned",
                    PerformedById = currentUser!.Id,
                    TargetUserId = user.Id,
                    Description = $"User {user.Email} assigned to role '{role}' by administrator.",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} assigned to role {Role} by {AdminId}.", id, role, currentUser.Id);
                TempData["SuccessMessage"] = $"Role updated for {user.FirstName} {user.LastName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}.", id);
                TempData["ErrorMessage"] = "Error assigning role.";
            }

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// Toggles the active/lockout status of a user.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            try
            {
                var isLocked = await _userManager.IsLockedOutAsync(user);
                if (isLocked)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    _logger.LogInformation("User {UserId} unlocked.", id);
                    TempData["SuccessMessage"] = "User activated successfully.";
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    _logger.LogInformation("User {UserId} locked.", id);
                    TempData["SuccessMessage"] = "User deactivated successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active status for user {UserId}.", id);
                TempData["ErrorMessage"] = "Error updating user status.";
            }

            return RedirectToAction(nameof(Users));
        }

        /// <summary>
        /// Lists all writer applications with filtering.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterApplications(WriterApplicationStatus? status = null)
        {
            try
            {
                IQueryable<WriterApplication> query = _context.WriterApplications
                    .Include(a => a.User)
                    .Include(a => a.ReviewedBy)
                    .OrderByDescending(a => a.SubmittedAt);

                if (status.HasValue)
                {
                    query = query.Where(a => a.Status == status.Value);
                }

                var applications = await query.ToListAsync();

                var vm = new WriterApplicationsViewModel
                {
                    Applications = applications,
                    CurrentFilter = status
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer applications.");
                TempData["ErrorMessage"] = "Error loading applications.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Displays details of a writer application with approve/reject actions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterApplicationDetails(int? id)
        {
            if (!id.HasValue) return NotFound();

            var application = await _context.WriterApplications
                .Include(a => a.User)
                .Include(a => a.ReviewedBy)
                .FirstOrDefaultAsync(a => a.Id == id.Value);

            if (application == null) return NotFound();

            return View(application);
        }

        /// <summary>
        /// Approves a writer application and assigns Writer role.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWriterApplication(int id, string? adminComments)
        {
            var application = await _context.WriterApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                application.Status = WriterApplicationStatus.Approved;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewedByAdminId = currentUser!.Id;
                application.AdminComments = adminComments;

                var user = application.User;
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userManager.AddToRoleAsync(user, "Writer");
                user.UserType = "Writer";
                user.IsActive = true;
                await _userManager.UpdateAsync(user);

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Writer Application Approved",
                    PerformedById = currentUser.Id,
                    TargetUserId = user.Id,
                    Description = $"Writer application for {user.Email} approved. Writer role assigned.",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Writer application {ApplicationId} approved.", id);

                await _notificationService.CreateNotificationAsync(
                    user.Id,
                    "Application Approved",
                    "Congratulations! Your writer application has been approved. You can now browse and apply to orders.",
                    NotificationType.WriterApproved,
                    application.Id.ToString());

                TempData["SuccessMessage"] = $"Writer application approved. {user.FirstName} {user.LastName} is now a writer.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving writer application {Id}.", id);
                TempData["ErrorMessage"] = "Error approving writer application.";
            }

            return RedirectToAction(nameof(WriterApplicationDetails), new { id });
        }

        /// <summary>
        /// Rejects a writer application with admin feedback.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWriterApplication(int id, string? adminComments)
        {
            var application = await _context.WriterApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                application.Status = WriterApplicationStatus.Rejected;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewedByAdminId = currentUser!.Id;
                application.AdminComments = adminComments;

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Writer Application Rejected",
                    PerformedById = currentUser.Id,
                    TargetUserId = application.UserId,
                    Description = $"Writer application for {application.User.Email} rejected. Reason: {adminComments}",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Writer application {ApplicationId} rejected.", id);

                await _notificationService.CreateNotificationAsync(
                    application.UserId,
                    "Application Rejected",
                    $"Your writer application has been reviewed and was not approved at this time. Admin feedback: {adminComments ?? "No additional details provided."}",
                    NotificationType.WriterApplicationRejected,
                    application.Id.ToString());

                TempData["SuccessMessage"] = "Writer application rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting writer application {Id}.", id);
                TempData["ErrorMessage"] = "Error rejecting writer application.";
            }

            return RedirectToAction(nameof(WriterApplicationDetails), new { id });
        }

        /// <summary>
        /// Suspends an approved writer's application/account.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendWriterApplication(int id, string? adminComments)
        {
            var application = await _context.WriterApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            try
            {
                application.Status = WriterApplicationStatus.Suspended;
                application.ReviewedAt = DateTime.UtcNow;
                application.ReviewedByAdminId = currentUser!.Id;
                application.AdminComments = adminComments;

                // Also deactivate the user
                application.User.IsActive = false;
                await _userManager.UpdateAsync(application.User);

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Writer Suspended",
                    PerformedById = currentUser.Id,
                    TargetUserId = application.UserId,
                    Description = $"Writer {application.User.Email} suspended. Reason: {adminComments}",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Writer application {ApplicationId} suspended.", id);

                TempData["SuccessMessage"] = "Writer has been suspended.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending writer application {Id}.", id);
                TempData["ErrorMessage"] = "Error suspending writer.";
            }

            return RedirectToAction(nameof(WriterApplicationDetails), new { id });
        }

        /// <summary>
        /// Displays all orders with administrative controls (assignment, applications).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderManagement(string? statusFilter = null)
        {
            try
            {
                IQueryable<Order> query = _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.AssignedWriter)
                    .Include(o => o.Applications)
                    .OrderByDescending(o => o.CreatedAt);

                if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, out var status))
                {
                    query = query.Where(o => o.Status == status).Cast<Order>();
                }

                var orders = await query.ToListAsync();

                var approvedWriters = await _userManager.GetUsersInRoleAsync("Writer");
                ViewBag.ApprovedWriters = approvedWriters.Where(w => w.IsActive && !w.IsDeleted).ToList();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order management.");
                TempData["ErrorMessage"] = "Error loading orders.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Admin views applicants for a specific order with assignment actions.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderApplicants(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.AssignedWriter)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var applicants = await _orderAssignmentService.GetApplicationsForOrderAsync(id);

            // Get automated writer recommendations for this order
            var recommendations = await _writerCapacityService.GetRecommendedWritersAsync(id, 10);
            ViewBag.Order = order;
            ViewBag.Recommendations = recommendations;
            return View(applicants);
        }

        /// <summary>
        /// Admin assigns a writer to an order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignWriterToOrder(int orderId, string writerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                await _orderAssignmentService.AssignWriterAsync(orderId, writerId, currentUser.Id);
                TempData["SuccessMessage"] = "Writer assigned to order successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning writer to order {OrderId}.", orderId);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(OrderApplicants), new { id = orderId });
        }

        /// <summary>
        /// Admin reassigns an order back to the marketplace.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignOrder(int orderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                await _orderAssignmentService.ReassignOrderAsync(orderId, currentUser.Id);
                TempData["SuccessMessage"] = "Order returned to marketplace.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning order {OrderId}.", orderId);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(OrderManagement));
        }

        /// <summary>
        /// Admin rejects a writer's application to a specific order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrderApplication(int applicationId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                await _orderAssignmentService.RejectApplicationAsync(applicationId, currentUser.Id);
                TempData["SuccessMessage"] = "Writer application rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting order application {AppId}.", applicationId);
                TempData["ErrorMessage"] = ex.Message;
            }

            // Find the order for redirect
            var app = await _context.OrderApplications.FindAsync(applicationId);
            if (app != null)
                return RedirectToAction(nameof(OrderApplicants), new { id = app.OrderId });

            return RedirectToAction(nameof(OrderManagement));
        }

        // ──────────────────────────────────────────────
        // Writer Resources (Knowledge Center) Management
        // ──────────────────────────────────────────────

        /// <summary>
        /// Lists all writer resources for admin management.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterResources(WriterResourceCategory? category = null)
        {
            var resources = await _writerResourceService.GetAllForAdminAsync(category);
            ViewBag.CurrentFilter = category;
            return View(resources);
        }

        /// <summary>
        /// Shows the create form for a new writer resource.
        /// </summary>
        [HttpGet]
        public IActionResult WriterResourceCreate()
        {
            ViewBag.Categories = Enum.GetValues<WriterResourceCategory>().ToList();
            return View(new WriterResource { IsActive = true, SortOrder = 0 });
        }

        /// <summary>
        /// Handles creation of a new writer resource.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriterResourceCreate(WriterResource model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = Enum.GetValues<WriterResourceCategory>().ToList();
                return View(model);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                await _writerResourceService.CreateAsync(model, currentUser.Id);
                TempData["SuccessMessage"] = $"Resource \"{model.Title}\" created successfully.";
                return RedirectToAction(nameof(WriterResources));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating writer resource.");
                TempData["ErrorMessage"] = "Error creating resource.";
                ViewBag.Categories = Enum.GetValues<WriterResourceCategory>().ToList();
                return View(model);
            }
        }

        /// <summary>
        /// Shows the edit form for an existing writer resource.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterResourceEdit(int id)
        {
            var resource = await _writerResourceService.GetByIdAsync(id);
            if (resource == null) return NotFound();

            ViewBag.Categories = Enum.GetValues<WriterResourceCategory>().ToList();
            return View(resource);
        }

        /// <summary>
        /// Handles update of an existing writer resource.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriterResourceEdit(WriterResource model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = Enum.GetValues<WriterResourceCategory>().ToList();
                return View(model);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var result = await _writerResourceService.UpdateAsync(model, currentUser.Id);
                if (result == null)
                {
                    TempData["ErrorMessage"] = "Resource not found.";
                    return RedirectToAction(nameof(WriterResources));
                }

                TempData["SuccessMessage"] = $"Resource \"{model.Title}\" updated successfully.";
                return RedirectToAction(nameof(WriterResources));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating writer resource {Id}.", model.Id);
                TempData["ErrorMessage"] = "Error updating resource.";
                ViewBag.Categories = Enum.GetValues<WriterResourceCategory>().ToList();
                return View(model);
            }
        }

        /// <summary>
        /// Deletes a writer resource.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriterResourceDelete(int id)
        {
            try
            {
                var deleted = await _writerResourceService.DeleteAsync(id);
                if (!deleted)
                {
                    TempData["ErrorMessage"] = "Resource not found.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Resource deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting writer resource {Id}.", id);
                TempData["ErrorMessage"] = "Error deleting resource.";
            }

            return RedirectToAction(nameof(WriterResources));
        }

        /// <summary>
        /// Toggles the active status of a writer resource.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WriterResourceToggleActive(int id)
        {
            try
            {
                var toggled = await _writerResourceService.ToggleActiveAsync(id);
                if (!toggled)
                {
                    TempData["ErrorMessage"] = "Resource not found.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Resource status updated.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling writer resource {Id}.", id);
                TempData["ErrorMessage"] = "Error updating resource status.";
            }

            return RedirectToAction(nameof(WriterResources));
        }

        // ──────────────────────────────────────────────
        // Admin Audit Center
        // ──────────────────────────────────────────────

        /// <summary>
        /// Single-screen command center showing all critical system alerts:
        /// failed payments, pending payouts, disputes, fraud alerts,
        /// flagged messages, writer penalties, support tickets, and system errors.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AuditCenter()
        {
            try
            {
                var vm = new AdminAuditCenterViewModel();

                // 1. Failed payments
                var failedPayments = await _context.Payments
                    .Include(p => p.Order).ThenInclude(o => o.Client)
                    .Where(p => p.Status == PaymentStatus.Failed)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                vm.FailedPaymentsCount = failedPayments.Count;
                vm.FailedPayments = failedPayments.Select(p => new PaymentSummary
                {
                    Id = p.Id,
                    OrderId = p.OrderId,
                    OrderNumber = p.Order?.OrderNumber ?? "",
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    TransactionReference = p.TransactionReference,
                    CreatedAt = p.CreatedAt,
                    ClientName = p.Order?.Client != null ? $"{p.Order.Client.FirstName} {p.Order.Client.LastName}" : null,
                    ClientEmail = p.Order?.Client?.Email
                }).ToList();

                // 2. Pending payouts (Writer navigation, PayoutStatus, RequestedDate, AdminNotes)
                var pendingPayouts = await _context.PayoutRequests
                    .Include(pr => pr.Writer)
                    .Where(pr => pr.Status == PayoutStatus.Pending)
                    .OrderByDescending(pr => pr.RequestedDate)
                    .Take(20)
                    .ToListAsync();

                vm.PendingPayoutsCount = pendingPayouts.Count;
                vm.PendingPayouts = pendingPayouts.Select(pr => new PayoutSummary
                {
                    Id = pr.Id,
                    Amount = pr.Amount,
                    WriterName = pr.Writer != null ? $"{pr.Writer.FirstName} {pr.Writer.LastName}" : null,
                    WriterEmail = pr.Writer?.Email,
                    PaymentMethod = "Wallet",
                    RequestedAt = pr.RequestedDate,
                    Notes = pr.AdminNotes
                }).ToList();

                // 3. Active disputes (Status is string "Open"/"InReview"/"Resolved", OpenedAt, Description)
                var disputes = await _context.OrderDisputes
                    .Include(d => d.Order)
                    .OrderByDescending(d => d.OpenedAt)
                    .Take(20)
                    .ToListAsync();

                vm.ActiveDisputesCount = disputes.Count(d => d.Status == "Open" || d.Status == "InReview");
                vm.ActiveDisputes = disputes.Select(d => new DisputeSummary
                {
                    Id = d.Id,
                    OrderId = d.OrderId,
                    OrderNumber = d.Order?.OrderNumber ?? "",
                    RaisedBy = d.ClientId,
                    Reason = d.Description,
                    Status = d.Status,
                    CreatedAt = d.OpenedAt
                }).ToList();

                // 4. Fraud alerts (DetectionType, DetectedAt, Severity)
                var fraudAlerts = await _context.Set<FraudIncident>()
                    .OrderByDescending(f => f.DetectedAt)
                    .Take(20)
                    .ToListAsync();

                vm.FraudAlertsCount = fraudAlerts.Count(f => !f.IsResolved);
                vm.FraudAlerts = fraudAlerts.Select(f => new FraudAlertSummary
                {
                    Id = f.Id,
                    UserName = f.UserId,
                    FraudType = f.DetectionType,
                    RiskScore = f.Severity == "PermanentBan" ? 100 : f.Severity == "TemporarySuspension" ? 60 : 30,
                    Description = f.Description,
                    CreatedAt = f.DetectedAt,
                    IsResolved = f.IsResolved
                }).ToList();

                // 5. Flagged messages (moderation violations)
                var violations = await _context.ModerationViolations
                    .Include(mv => mv.User)
                    .OrderByDescending(mv => mv.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                vm.FlaggedMessagesCount = violations.Count;
                vm.FlaggedMessages = violations.Select(mv => new FlaggedMessageSummary
                {
                    Id = mv.Id,
                    SenderName = mv.User != null ? $"{mv.User.FirstName} {mv.User.LastName}" : null,
                    SenderEmail = mv.User?.Email,
                    ContentPreview = mv.Description,
                    SentAt = mv.CreatedAt,
                    ConversationTitle = mv.ViolationType
                }).ToList();

                // 6. Writer penalties (PointsAdded/PointsRemoved instead of PenaltyAmount/IsPaid)
                var penalties = await _context.WriterPenaltyLogs
                    .Include(wp => wp.Writer)
                    .OrderByDescending(wp => wp.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                vm.WriterPenaltiesCount = penalties.Count;
                vm.WriterPenalties = penalties.Select(wp => new WriterPenaltySummary
                {
                    Id = wp.Id,
                    WriterName = wp.Writer != null ? $"{wp.Writer.FirstName} {wp.Writer.LastName}" : null,
                    WriterEmail = wp.Writer?.Email,
                    PenaltyAmount = wp.PointsRemoved,
                    Reason = wp.Reason ?? wp.Action,
                    CreatedAt = wp.CreatedAt,
                    IsPaid = false
                }).ToList();

                // 7. Client complaints (open support tickets — Creator navigation, TicketStatus enum)
                var openTickets = await _context.SupportTickets
                    .Include(st => st.Creator)
                    .Where(st => st.Status != TicketStatus.Resolved)
                    .OrderByDescending(st => st.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                vm.ClientComplaintsCount = openTickets.Count;
                vm.ClientComplaints = openTickets.Select(st => new SupportTicketSummary
                {
                    Id = st.Id,
                    UserName = st.Creator != null ? $"{st.Creator.FirstName} {st.Creator.LastName}" : null,
                    UserEmail = st.Creator?.Email,
                    Subject = st.Subject,
                    Department = st.Department.ToString(),
                    Status = st.Status.ToString(),
                    Priority = "",
                    CreatedAt = st.CreatedAt
                }).ToList();

                // 8. System errors (ErrorMessage, Timestamp)
                var errors = await _context.ErrorLogs
                    .OrderByDescending(e => e.Timestamp)
                    .Take(20)
                    .ToListAsync();

                vm.SystemErrorsCount = errors.Count(e => !e.IsResolved);
                vm.SystemErrors = errors.Select(e => new ErrorLogSummary
                {
                    Id = e.Id,
                    Category = e.Category ?? "General",
                    Message = e.ErrorMessage,
                    UserName = e.UserId,
                    Url = e.Url,
                    CreatedAt = e.Timestamp,
                    IsResolved = e.IsResolved
                }).ToList();

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin audit center.");
                TempData["ErrorMessage"] = "Error loading audit center.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // ──────────────────────────────────────────────
        // QA Pipeline
        // ──────────────────────────────────────────────

        /// <summary>Lists orders pending QA review.</summary>
        [HttpGet]
        public async Task<IActionResult> PendingQa()
        {
            var orders = await _context.Orders.Include(o => o.Client).Include(o => o.AssignedWriter)
                .Where(o => o.Status == OrderStatus.PendingQA)
                .OrderByDescending(o => o.UpdatedAt).AsNoTracking().ToListAsync();
            return View(orders);
        }

        /// <summary>Shows QA review form for an order.</summary>
        [HttpGet]
        public async Task<IActionResult> QaReview(int orderId)
        {
            var order = await _context.Orders.Include(o => o.Client).Include(o => o.AssignedWriter).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.Status != OrderStatus.PendingQA) return NotFound();
            ViewBag.Order = order;
            ViewBag.Submissions = await _workDeliveryService.GetSubmissionsAsync(orderId);
            return View(new QaReview { OrderId = orderId });
        }

        /// <summary>Submits a QA review — approves/rejects and delivers to client.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QaReview(QaReview model, IFormFile? plagiarismReport)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return Challenge();
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId);
                if (order == null || order.Status != OrderStatus.PendingQA) throw new InvalidOperationException("Invalid order or not pending QA.");
                model.ReviewerId = admin.Id;
                model.CreatedAt = DateTime.UtcNow;
                if (plagiarismReport != null && plagiarismReport.Length > 0)
                {
                    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "qa-reports", model.OrderId.ToString());
                    Directory.CreateDirectory(dir);
                    var safe = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{plagiarismReport.FileName}";
                    using var s = new FileStream(Path.Combine(dir, safe), FileMode.Create); await plagiarismReport.CopyToAsync(s);
                    model.PlagiarismReportPath = $"/uploads/qa-reports/{model.OrderId}/{safe}";
                }
                _context.QaReviews.Add(model);
                if (model.IsApproved)
                {
                    order.Status = OrderStatus.Delivered; order.UpdatedAt = DateTime.UtcNow;
                    await _notificationService.CreateNotificationAsync(order.ClientId, "Work Delivered", $"Order {order.OrderNumber} has passed QA.", NotificationType.OrderCompleted, order.Id.ToString());
                }
                _context.AuditLogs.Add(new AuditLog { Action = model.IsApproved ? "QA Approved" : "QA Rejected", PerformedById = admin.Id, TargetUserId = order.AssignedWriterId, Description = $"QA {(model.IsApproved ? "approved" : "rejected")} for order {order.OrderNumber}", CreatedDate = DateTime.UtcNow });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = model.IsApproved ? "QA approved. Order delivered to client." : "QA review recorded.";
            }
            catch (Exception ex) { _logger.LogError(ex, "QA review error"); TempData["ErrorMessage"] = ex.Message; }
            return RedirectToAction(nameof(PendingQa));
        }

        // ──────────────────────────────────────────────
        // Writer Rankings Management
        // ──────────────────────────────────────────────

        /// <summary>
        /// Lists all writer rankings with metrics. Admins can override ranks here.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterRankings(WriterRank? filterRank = null)
        {
            var rankings = await _writerRankingService.GetAllAsync();
            if (filterRank.HasValue)
                rankings = rankings.Where(r => r.CurrentRank == filterRank.Value).ToList();

            ViewBag.CurrentFilter = filterRank;
            ViewBag.Criteria = _writerRankingService.GetPromotionCriteria();
            return View(rankings);
        }

        /// <summary>
        /// Admin manually overrides a writer's rank.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OverrideWriterRank(string writerId, WriterRank newRank, string? notes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                await _writerRankingService.OverrideRankAsync(writerId, newRank, currentUser.Id, notes);
                TempData["SuccessMessage"] = $"Writer rank overridden to {newRank}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error overriding rank for writer {WriterId}.", writerId);
                TempData["ErrorMessage"] = "Error overriding rank.";
            }

            return RedirectToAction(nameof(WriterRankings));
        }

        /// <summary>
        /// Clear admin override and resume auto-promotion.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWriterRankOverride(string writerId)
        {
            try
            {
                await _writerRankingService.ClearOverrideAsync(writerId);
                TempData["SuccessMessage"] = "Admin override cleared. Auto-promotion resumed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing rank override for writer {WriterId}.", writerId);
                TempData["ErrorMessage"] = "Error clearing override.";
            }

            return RedirectToAction(nameof(WriterRankings));
        }

        /// <summary>
        /// Force-evaluate the writer's rank based on current metrics.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecomputeWriterRank(string writerId)
        {
            try
            {
                await _writerRankingService.UpdateMetricsOnCompletionAsync(writerId);
                var newRank = await _writerRankingService.EvaluateAndApplyRankAsync(writerId);
                TempData["SuccessMessage"] = $"Writer rank re-evaluated to {newRank}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recomputing rank for writer {WriterId}.", writerId);
                TempData["ErrorMessage"] = "Error recomputing rank.";
            }

            return RedirectToAction(nameof(WriterRankings));
        }

        // ──────────────────────────────────────────────
        // Order Monitoring / Escalation Dashboard
        // ──────────────────────────────────────────────

        /// <summary>
        /// Escalation dashboard showing active monitoring alerts.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EscalationDashboard(MonitoringAlertType? typeFilter = null)
        {
            try
            {
                var alerts = await _context.MonitoringAlerts
                    .Include(a => a.Order)
                    .Include(a => a.Writer)
                    .Include(a => a.AcknowledgedBy)
                    .AsNoTracking()
                    .ToListAsync();

                var activeAlerts = alerts.Where(a => !a.IsAcknowledged && a.ResolvedAt == null).ToList();
                var recentResolved = alerts.Where(a => a.ResolvedAt != null)
                    .OrderByDescending(a => a.ResolvedAt).Take(10).ToList();

                var model = new EscalationDashboardViewModel
                {
                    ActiveAlerts = typeFilter.HasValue
                        ? activeAlerts.Where(a => a.AlertType == typeFilter.Value).ToList()
                        : activeAlerts,
                    RecentResolvedAlerts = recentResolved,
                    TotalActiveAlerts = activeAlerts.Count,
                    NoApplicantAlerts = activeAlerts.Count(a => a.AlertType == MonitoringAlertType.NoApplicantsAfter2Hours),
                    UrgentNoApplicantAlerts = activeAlerts.Count(a => a.AlertType == MonitoringAlertType.UrgentNoApplicantsAfter30Min),
                    WriterInactiveAlerts = activeAlerts.Count(a => a.AlertType == MonitoringAlertType.WriterInactive24Hours),
                    MilestoneOverdueAlerts = activeAlerts.Count(a => a.AlertType == MonitoringAlertType.MilestoneOverdue),
                    RevisionOverdueAlerts = activeAlerts.Count(a => a.AlertType == MonitoringAlertType.RevisionOverdue),
                    CurrentTypeFilter = typeFilter
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading escalation dashboard.");
                TempData["ErrorMessage"] = "Error loading escalation dashboard.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Acknowledge a monitoring alert.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcknowledgeAlert(int alertId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                var alert = await _context.MonitoringAlerts.FindAsync(alertId);
                if (alert == null) return NotFound();

                alert.IsAcknowledged = true;
                alert.AcknowledgedById = currentUser.Id;
                alert.AcknowledgedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Alert acknowledged.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert {AlertId}.", alertId);
                TempData["ErrorMessage"] = "Error acknowledging alert.";
            }

            return RedirectToAction(nameof(EscalationDashboard));
        }

        /// <summary>
        /// Resolve a monitoring alert.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveAlert(int alertId)
        {
            try
            {
                var alert = await _context.MonitoringAlerts.FindAsync(alertId);
                if (alert == null) return NotFound();

                alert.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Alert resolved.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert {AlertId}.", alertId);
                TempData["ErrorMessage"] = "Error resolving alert.";
            }

            return RedirectToAction(nameof(EscalationDashboard));
        }

        // ════════════════════════════════════════════════════════════
        // INTELLIGENT WRITER MATCHING & AUTO ASSIGNMENT ENGINE
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Shows the Writer Recommendation Panel for a specific order.
        /// Displays top N ranked writers with match scores and explanations.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterRecommendations(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.AssignedWriter)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                var recommendations = await _writerMatchingService.GetTopRecommendationsAsync(id, 10);

                var config = new Models.Configuration.MatchingConfiguration();
                var configSetting = await _context.PlatformSettings
                    .FirstOrDefaultAsync(s => s.Key == "MatchingConfiguration");
                if (configSetting?.Value != null)
                {
                    try { config = System.Text.Json.JsonSerializer.Deserialize<Models.Configuration.MatchingConfiguration>(configSetting.Value) ?? config; }
                    catch { }
                }

                var vm = new WriterRecommendationsViewModel
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderTitle = order.Title,
                    OrderSubject = order.Subject,
                    AcademicLevel = order.AcademicLevel.ToString(),
                    Priority = order.Priority.ToString(),
                    Budget = order.Budget,
                    IsAssigned = order.AssignedWriterId != null,
                    AssignedWriterName = order.AssignedWriter != null ? $"{order.AssignedWriter.FirstName} {order.AssignedWriter.LastName}" : null,
                    AssignedWriterId = order.AssignedWriterId,
                    Recommendations = recommendations.Select((s, idx) => new RecommendationItem
                    {
                        Rank = idx + 1,
                        WriterId = s.WriterId,
                        WriterName = s.Writer != null ? $"{s.Writer.FirstName} {s.Writer.LastName}" : "Unknown",
                        WriterEmail = s.Writer?.Email ?? "",
                        MatchPercentage = s.MatchPercentage,
                        Explanation = s.Explanation ?? _writerMatchingService.GenerateExplanation(s),
                        Rating = s.RatingScore / 20.0, // Convert 0-100 to 0-5 scale
                        ReliabilityScore = s.ReliabilityScore,
                        CapacityPercent = 100 - (int)s.CapacityScore,
                        QualityScore = s.QualityScore,
                        CompletedOrders = s.Writer?.TotalCompletedOrders ?? 0
                    }).ToList(),
                    AutoAssignment = new AutoAssignmentStatus
                    {
                        IsEnabled = config.AutoAssignmentMode == Models.Enums.AutoAssignmentMode.AutomaticAssignment,
                        IsRecommendationOnly = config.AutoAssignmentMode == Models.Enums.AutoAssignmentMode.RecommendationOnly,
                        StatusMessage = config.AutoAssignmentMode switch
                        {
                            Models.Enums.AutoAssignmentMode.Disabled => "Auto-assignment is disabled. Manual assignment only.",
                            Models.Enums.AutoAssignmentMode.RecommendationOnly => "System shows recommendations only. Admin must assign writers manually.",
                            Models.Enums.AutoAssignmentMode.AutomaticAssignment => "Auto-assignment is enabled. Highest-ranked eligible writer will be auto-assigned when requirements are met.",
                            _ => "Auto-assignment status unknown."
                        },
                        CanAutoAssign = config.AutoAssignmentMode == Models.Enums.AutoAssignmentMode.AutomaticAssignment
                            && order.AssignedWriterId == null
                            && recommendations.Any()
                    }
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer recommendations for order {OrderId}.", id);
                TempData["ErrorMessage"] = "Error loading writer recommendations.";
                return RedirectToAction(nameof(OrderManagement));
            }
        }

        /// <summary>
        /// Execute auto-assignment for an order.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteAutoAssign(int orderId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            try
            {
                var (assigned, writerId, message) = await _writerMatchingService.TryAutoAssignAsync(orderId, currentUser.Id);
                if (assigned)
                {
                    TempData["SuccessMessage"] = message;

                    _context.AuditLogs.Add(new AuditLog
                    {
                        Action = "Auto-Assignment Executed",
                        PerformedById = currentUser.Id,
                        TargetUserId = writerId,
                        Description = $"Admin {currentUser.Email} triggered auto-assignment for order #{orderId}. {message}",
                        CreatedDate = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["InfoMessage"] = message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing auto-assignment for order {OrderId}.", orderId);
                TempData["ErrorMessage"] = "Error executing auto-assignment.";
            }

            return RedirectToAction(nameof(WriterRecommendations), new { id = orderId });
        }

        /// <summary>
        /// Matching Analytics Dashboard - shows matching engine performance metrics.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MatchingAnalytics()
        {
            try
            {
                var analytics = await _writerMatchingService.GetMatchingAnalyticsAsync();
                return View(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading matching analytics.");
                TempData["ErrorMessage"] = "Error loading matching analytics.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // ════════════════════════════════════════════════
        // ADMIN CONVERSATIONS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Lists all conversations for admin monitoring.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Conversations()
        {
            try
            {
                var conversations = await _context.Conversations
                    .Include(c => c.Order)
                    .Include(c => c.Participants)
                    .ThenInclude(p => p.User)
                    .OrderByDescending(c => c.LastMessageDate)
                    .Take(50)
                    .ToListAsync();

                return View(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversations.");
                TempData["ErrorMessage"] = "Error loading conversations.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // ════════════════════════════════════════════════
        // ADMIN PAYMENTS (read-only placeholder)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Payments administration. Placeholder until payment providers are configured.
        /// </summary>
        [HttpGet]
        public IActionResult Payments()
        {
            return View();
        }

        // ════════════════════════════════════════════════
        // ADMIN CONTACT MESSAGES
        // ════════════════════════════════════════════════

        /// <summary>
        /// Lists all contact messages from the public contact form.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ContactMessages()
        {
            try
            {
                var messages = await _context.ContactMessages
                    .OrderByDescending(m => m.SubmittedAt)
                    .Take(100)
                    .AsNoTracking()
                    .ToListAsync();

                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contact messages.");
                TempData["ErrorMessage"] = "Error loading contact messages.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Shows details of a single contact message.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ContactMessageDetails(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var message = await _context.ContactMessages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (message == null) return NotFound();

                return View(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contact message details.");
                TempData["ErrorMessage"] = "Error loading message details.";
                return RedirectToAction(nameof(ContactMessages));
            }
        }

        // ════════════════════════════════════════════════
        // ADMIN AUDIT LOGS (via AuditCenter)
        // ════════════════════════════════════════════════

        /// <summary>
        /// Redirects to the full Audit Center page.
        /// </summary>
        [HttpGet]
        public IActionResult AuditLogs()
        {
            return RedirectToAction(nameof(AuditCenter));
        }

        // ════════════════════════════════════════════════
        // ADMIN NOTIFICATIONS
        // ════════════════════════════════════════════════

        /// <summary>
        /// Shows notifications relevant to the current admin.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var notifications = await _context.Notifications
                    .Where(n => n.UserId == currentUser.Id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(100)
                    .AsNoTracking()
                    .ToListAsync();

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin notifications.");
                TempData["ErrorMessage"] = "Error loading notifications.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Toggle auto-assignment mode via admin action.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAutoAssignmentMode(Models.Enums.AutoAssignmentMode mode)
        {
            var setting = await _context.PlatformSettings
                .FirstOrDefaultAsync(s => s.Key == "MatchingConfiguration");

            var config = new Models.Configuration.MatchingConfiguration();
            if (setting?.Value != null)
            {
                try { config = System.Text.Json.JsonSerializer.Deserialize<Models.Configuration.MatchingConfiguration>(setting.Value) ?? config; }
                catch { }
            }

            config.AutoAssignmentMode = mode;

            if (setting == null)
            {
                setting = new PlatformSetting
                {
                    Key = "MatchingConfiguration",
                    Value = System.Text.Json.JsonSerializer.Serialize(config),
                    Category = "Matching",
                    IsEditable = true
                };
                _context.PlatformSettings.Add(setting);
            }
            else
            {
                setting.Value = System.Text.Json.JsonSerializer.Serialize(config);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Auto-assignment mode set to {mode}.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
