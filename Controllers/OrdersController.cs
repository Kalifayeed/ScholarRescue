using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;
using ScholarRescue.Services;
using ScholarRescue.ViewModels.Order;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller responsible for managing orders in the Scholar Rescue platform.
    /// Provides full CRUD operations and a client dashboard.
    /// Pricing is automatic; clients never enter budgets.
    /// </summary>
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrdersController> _logger;
        private readonly IPricingService _pricingService;
        private readonly IWalletService _walletService;
        private readonly IVerificationService _verificationService;
        private readonly IConfigurationService _configurationService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWorkDeliveryService _workDeliveryService;
        private readonly INotificationService _notificationService;

        public OrdersController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OrdersController> logger,
            IPricingService pricingService,
            IWalletService walletService,
            IVerificationService verificationService,
            IConfigurationService configurationService,
            SignInManager<ApplicationUser> signInManager,
            IWorkDeliveryService workDeliveryService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _pricingService = pricingService;
            _walletService = walletService;
            _verificationService = verificationService;
            _configurationService = configurationService;
            _signInManager = signInManager;
            _workDeliveryService = workDeliveryService;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                IQueryable<Order> ordersQuery = _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.AssignedWriter)
                    .AsNoTracking();

                if (User.IsInRole(RoleNames.Administrator))
                {
                    // Admins see all orders
                }
                else if (User.IsInRole(RoleNames.Client))
                {
                    ordersQuery = ordersQuery.Where(o => o.ClientId == currentUser.Id);
                }
                else if (User.IsInRole(RoleNames.Writer))
                {
                    ordersQuery = ordersQuery.Where(o => o.AssignedWriterId == currentUser.Id);
                }

                var orders = await ordersQuery
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new OrderIndexViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Title = o.Title,
                        ClientName = o.Client.FirstName + " " + o.Client.LastName,
                        WriterName = o.AssignedWriter != null ? o.AssignedWriter.FirstName + " " + o.AssignedWriter.LastName : null,
                        Status = o.Status,
                        Deadline = o.Deadline,
                        Pages = o.Pages,
                        Budget = o.Budget,
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving orders list.");
                TempData["ErrorMessage"] = "An error occurred while loading orders. Please try again.";
                return View(new List<OrderIndexViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.AssignedWriter)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole(RoleNames.Administrator) &&
                    order.ClientId != currentUser.Id &&
                    order.AssignedWriterId != currentUser.Id)
                {
                    return Forbid();
                }

                var breakdown = _pricingService.GetPriceBreakdown(
                    order.AcademicLevel, order.Pages, order.Deadline);

                var viewModel = new OrderDetailsViewModel
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    Title = order.Title,
                    Description = order.Description,
                    Subject = order.Subject,
                    AcademicLevel = order.AcademicLevel,
                    AcademicLevelName = breakdown.AcademicLevelName,
                    CitationFormat = order.CitationFormat,
                    CitationFormatName = order.CitationFormat.ToDisplayName(),
                    Deadline = order.Deadline,
                    Pages = order.Pages,
                    WordCount = order.WordCount,
                    Budget = order.Budget,
                    BaseRatePerPage = breakdown.BaseRatePerPage,
                    SurchargePerPage = breakdown.UrgencySurchargePerPage,
                    BasePrice = breakdown.BaseTotal,
                    UrgencySurcharge = breakdown.UrgencyTotal,
                    CommissionAmount = order.CommissionAmount,
                    WriterEarnings = order.WriterEarnings,
                    NumberOfSources = order.NumberOfSources,
                    Priority = order.Priority,
                    Status = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    PaystackReference = order.PaystackReference,
                    PaymentDate = order.PaymentDate,
                    ClientName = $"{order.Client.FirstName} {order.Client.LastName}",
                    ClientEmail = order.Client.Email ?? string.Empty,
                    WriterName = order.AssignedWriter != null
                        ? $"{order.AssignedWriter.FirstName} {order.AssignedWriter.LastName}"
                        : null,
                    WriterEmail = order.AssignedWriter?.Email,
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt,
                    CompletedAt = order.CompletedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving order details for ID {OrderId}.", id);
                TempData["ErrorMessage"] = "An error occurred while loading order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public IActionResult Create()
        {
            return View(new CreateOrderViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                // Generate order number
                var lastOrder = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();

                int nextNumber = (lastOrder?.Id ?? 0) + 1;
                string orderNumber = $"SR-{DateTime.UtcNow.Year}-{nextNumber:D6}";

                // Handle subject: if "Other", use OtherSubject
                string subject = viewModel.Subject;
                if (subject == "Other" && !string.IsNullOrWhiteSpace(viewModel.OtherSubject))
                {
                    subject = viewModel.OtherSubject;
                }

                // Auto pricing
                var wordCount = _pricingService.CalculateWordCount(viewModel.Pages);
                var budget = _pricingService.CalculatePrice(
                    viewModel.AcademicLevel, viewModel.Pages, viewModel.Deadline);
                var commissionRate = await _configurationService.GetCommissionRateAsync();
                var commission = Math.Round(budget * commissionRate, 2);
                var writerEarnings = budget - commission;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    Subject = subject,
                    AcademicLevel = viewModel.AcademicLevel,
                    CitationFormat = viewModel.CitationFormat,
                    Deadline = viewModel.Deadline,
                    Pages = viewModel.Pages,
                    WordCount = wordCount,
                    Budget = budget,
                    CommissionAmount = commission,
                    WriterEarnings = writerEarnings,
                    NumberOfSources = viewModel.NumberOfSources,
                    Priority = PriorityLevel.Normal,
                    Status = OrderStatus.PendingPayment,
                    ClientId = currentUser.Id,
                    IsMarketplaceOpen = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Order history
                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    OldStatus = OrderStatus.PendingPayment,
                    NewStatus = OrderStatus.PendingPayment,
                    ChangedById = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Order created, pending payment"
                });

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Order Created (Pending Payment)",
                    PerformedById = currentUser.Id,
                    TargetUserId = currentUser.Id,
                    Description = $"Order {orderNumber} created, pending payment",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderNumber} created, redirecting to payment.", orderNumber);

                return RedirectToAction("Checkout", "Payments", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order.");
                TempData["ErrorMessage"] = "An error occurred while creating the order. Please try again.";
                return View(viewModel);
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole(RoleNames.Administrator) && order.ClientId != currentUser.Id)
                    return Forbid();

                var viewModel = new EditOrderViewModel
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    Title = order.Title,
                    Description = order.Description,
                    Subject = order.Subject,
                    AcademicLevel = order.AcademicLevel,
                    CitationFormat = order.CitationFormat,
                    Deadline = order.Deadline,
                    Pages = order.Pages,
                    WordCount = order.WordCount,
                    Budget = order.Budget,
                    CommissionAmount = order.CommissionAmount,
                    WriterEarnings = order.WriterEarnings,
                    NumberOfSources = order.NumberOfSources,
                    Status = order.Status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order {OrderId} for edit.", id);
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Edit(int id, EditOrderViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();
            if (!ModelState.IsValid) return View(viewModel);

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole(RoleNames.Administrator) && order.ClientId != currentUser.Id)
                    return Forbid();

                order.Title = viewModel.Title;
                order.Description = viewModel.Description;
                order.Subject = viewModel.Subject;
                order.AcademicLevel = viewModel.AcademicLevel;
                order.CitationFormat = viewModel.CitationFormat;
                order.Deadline = viewModel.Deadline;
                order.Pages = viewModel.Pages;
                order.NumberOfSources = viewModel.NumberOfSources;
                order.WordCount = _pricingService.CalculateWordCount(viewModel.Pages);
                order.Budget = _pricingService.CalculatePrice(viewModel.AcademicLevel, viewModel.Pages, viewModel.Deadline);
                var commissionRateForEdit = await _configurationService.GetCommissionRateAsync();
                order.CommissionAmount = Math.Round(order.Budget * commissionRateForEdit, 2);
                order.WriterEarnings = order.Budget - order.CommissionAmount;
                order.UpdatedAt = DateTime.UtcNow;

                if (User.IsInRole(RoleNames.Administrator))
                {
                    order.Status = viewModel.Status;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderNumber} updated.", order.OrderNumber);
                TempData["SuccessMessage"] = $"Order {order.OrderNumber} has been updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}.", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the order.");
                return View(viewModel);
            }
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Client)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole(RoleNames.Administrator) && order.ClientId != currentUser.Id)
                    return Forbid();

                if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.PendingReview)
                {
                    TempData["ErrorMessage"] = "Only orders in Draft or Pending Review status can be deleted.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new OrderDetailsViewModel
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    Title = order.Title,
                    Description = order.Description,
                    Subject = order.Subject,
                    AcademicLevel = order.AcademicLevel,
                    Deadline = order.Deadline,
                    Pages = order.Pages,
                    WordCount = order.WordCount,
                    Budget = order.Budget,
                    Priority = order.Priority,
                    Status = order.Status,
                    ClientName = $"{order.Client.FirstName} {order.Client.LastName}",
                    ClientEmail = order.Client.Email ?? string.Empty,
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order {OrderId} for delete.", id);
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole(RoleNames.Administrator) && order.ClientId != currentUser.Id)
                    return Forbid();

                if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.PendingReview)
                {
                    TempData["ErrorMessage"] = "Only orders in Draft or Pending Review status can be deleted.";
                    return RedirectToAction(nameof(Index));
                }

                string orderNumber = order.OrderNumber;
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderNumber} deleted.", orderNumber);
                TempData["SuccessMessage"] = $"Order {orderNumber} deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}.", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the order.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ═══════════════════════════════════════════════
        // GUEST ORDER CREATION (No Authentication Required)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Guest users can create an order with automatic account registration.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GuestCreate()
        {
            return View(new GuestOrderViewModel
            {
                Deadline = DateTime.UtcNow.AddDays(7)
            });
        }

        /// <summary>
        /// Handle guest order submission: create account, create order, log user in.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuestCreate(GuestOrderViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Check for duplicate email
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "An account with this email already exists. Please log in.");
                    return View(model);
                }

                // 1. Create client account
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    UserType = RoleNames.Client,
                    IsActive = true,
                    RegistrationCompleted = true,
                    RegistrationSource = "OrderFlow",
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var err in createResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }

                // Assign Client role
                await _userManager.AddToRoleAsync(user, RoleNames.Client);

                // 2. Create the order
                var lastOrder = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                int nextNumber = (lastOrder?.Id ?? 0) + 1;
                string orderNumber = $"SR-{DateTime.UtcNow.Year}-{nextNumber:D6}";
                var wordCount = _pricingService.CalculateWordCount(model.Pages);
                var budget = _pricingService.CalculatePrice(model.AcademicLevel, model.Pages, model.Deadline);
                var commissionRate = await _configurationService.GetCommissionRateAsync();
                var commission = Math.Round(budget * commissionRate, 2);
                var writerEarnings = budget - commission;

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    Title = model.Title,
                    Description = model.Description,
                    Subject = model.Subject,
                    AcademicLevel = model.AcademicLevel,
                    CitationFormat = model.CitationFormat,
                    Deadline = model.Deadline,
                    Pages = model.Pages,
                    WordCount = wordCount,
                    Budget = budget,
                    CommissionAmount = commission,
                    WriterEarnings = writerEarnings,
                    NumberOfSources = model.NumberOfSources,
                    Priority = PriorityLevel.Normal,
                    Status = model.SaveAsDraft ? OrderStatus.Draft : OrderStatus.PendingPayment,
                    ClientId = user.Id,
                    IsMarketplaceOpen = false,
                    IsDraft = model.SaveAsDraft,
                    DraftSavedAt = model.SaveAsDraft ? DateTime.UtcNow : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Order history
                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    OldStatus = order.Status,
                    NewStatus = order.Status,
                    ChangedById = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    Notes = model.SaveAsDraft ? "Draft order created" : "Order created via guest flow"
                });

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = model.SaveAsDraft ? "Draft Created" : "Order Created",
                    PerformedById = user.Id,
                    TargetUserId = user.Id,
                    Description = $"Client registered through order flow. Order {orderNumber} created.",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // 3. Log the user in automatically
                await _signInManager.SignInAsync(user, isPersistent: true);

                // 4. Send welcome email (email confirmation optional for clients)
                await _verificationService.SendClientWelcomeEmailAsync(model.Email, $"{model.FirstName} {model.LastName}");

                _logger.LogInformation("Guest user {Email} created account and order {OrderNumber}", model.Email, orderNumber);

                TempData["SuccessMessage"] = model.SaveAsDraft
                    ? "Your draft has been saved. You can resume it anytime from your dashboard."
                    : "Account created and order placed! Proceed to payment.";

                if (model.SaveAsDraft)
                    return RedirectToAction(nameof(Dashboard));

                return RedirectToAction("Checkout", "Payments", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing guest order creation.");
                ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Shows all bids submitted on a client's order.
        /// Only the order owner (client) and administrators can view bids.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Bids(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                // Ownership check: only the client who owns the order or admin can view bids
                if (!User.IsInRole(RoleNames.Administrator) && order.ClientId != currentUser.Id)
                    return Forbid();

                bool isAdmin = User.IsInRole(RoleNames.Administrator);

                var rawBids = await _context.OrderBids
                    .Include(b => b.Writer)
                    .Where(b => b.OrderId == order.Id)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();

                // Privacy: clients see anonymous writer labels; admin sees real identities
                int writerCounter = 1;
                var bids = rawBids.Select(b =>
                {
                    string displayName;
                    string? email = null;

                    if (isAdmin)
                    {
                        // Admin sees real writer identity for vetting
                        displayName = b.Writer.FirstName + " " + b.Writer.LastName;
                        email = b.Writer.Email;
                    }
                    else
                    {
                        // Clients see anonymous labels to preserve writer privacy until assignment
                        displayName = $"Verified Writer #{writerCounter++}";
                    }

                    return new BidItemViewModel
                    {
                        BidId = b.Id,
                        WriterId = b.WriterId,
                        WriterDisplayName = displayName,
                        WriterEmail = email,
                        Amount = b.Amount,
                        Message = b.Message,
                        EstimatedDeliveryDate = b.EstimatedDeliveryDate,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    };
                }).ToList();

                var viewModel = new OrderBidsViewModel
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderTitle = order.Title,
                    OrderSubject = order.Subject,
                    IsClientOwner = order.ClientId == currentUser.Id,
                    IsAdmin = isAdmin,
                    Bids = bids
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bids for order {OrderId}.", id);
                TempData["ErrorMessage"] = "An error occurred while loading bids.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ═══════════════════════════════════════════════
        // ASSIGNED ORDER WORKSPACE (Phase 4)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Assigned Order Workspace for client-writer collaboration.
        /// Route: GET /Orders/Workspace/{id}
        /// Access: Order owner (Client), Assigned Writer, or Admin.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Workspace(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.AssignedWriter)
                    .Include(o => o.Attachments)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                // Access control: Client (owner), Assigned Writer, or Admin
                bool isAdmin = User.IsInRole(RoleNames.Administrator);
                bool isClient = order.ClientId == currentUser.Id;
                bool isAssignedWriter = order.AssignedWriterId == currentUser.Id;

                if (!isAdmin && !isClient && !isAssignedWriter)
                    return Forbid();

                // Workspace is only meaningful for assigned orders
                if (order.AssignedWriterId == null && !isAdmin)
                {
                    TempData["ErrorMessage"] = "This order has not been assigned yet. The workspace is available once a writer is assigned.";
                    return RedirectToAction("Details", new { id });
                }

                // Find the existing conversation for this order
                var conversation = await _context.Conversations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.OrderId == order.Id);

                // Build price breakdown for display labels
                var breakdown = _pricingService.GetPriceBreakdown(
                    order.AcademicLevel, order.Pages, order.Deadline);

                // Determine privacy-safe labels
                string otherPartyLabel;
                string otherPartyName;
                string myRole;

                if (isAdmin)
                {
                    otherPartyLabel = "Client / Assigned Writer";
                    otherPartyName = $"{order.Client.FirstName} {order.Client.LastName} / {(order.AssignedWriter != null ? $"{order.AssignedWriter.FirstName} {order.AssignedWriter.LastName}" : "Unassigned")}";
                    myRole = RoleNames.Administrator;
                }
                else if (isClient)
                {
                    otherPartyLabel = "Assigned Writer";
                    otherPartyName = order.AssignedWriter != null
                        ? $"{order.AssignedWriter.FirstName} {order.AssignedWriter.LastName}"
                        : "Awaiting assignment";
                    myRole = RoleNames.Client;
                }
                else // Assigned Writer
                {
                    otherPartyLabel = "Client";
                    otherPartyName = $"{order.Client.FirstName} {order.Client.LastName}";
                    myRole = RoleNames.Writer;
                }

                // Load submissions and revision requests
                var submissions = await _workDeliveryService.GetSubmissionsAsync(order.Id);
                var revisions = await _workDeliveryService.GetOrderRevisionsAsync(order.Id);

                // Determine action permissions
                bool canSubmitWork = isAssignedWriter && !isAdmin &&
                    (order.Status == OrderStatus.InProgress || order.Status == OrderStatus.Assigned ||
                     order.Status == OrderStatus.RevisionRequested);

                bool canRequestRevision = isClient && !isAdmin &&
                    (order.Status == OrderStatus.DraftSubmitted || order.Status == OrderStatus.RevisionSubmitted ||
                     order.Status == OrderStatus.FinalSubmitted);

                bool canAcceptWork = isClient && !isAdmin &&
                    (order.Status == OrderStatus.DraftSubmitted || order.Status == OrderStatus.RevisionSubmitted ||
                     order.Status == OrderStatus.FinalSubmitted);

                // Admin can do everything if order is submitted
                if (isAdmin)
                {
                    canSubmitWork = true;
                    canRequestRevision = order.Status == OrderStatus.DraftSubmitted ||
                                         order.Status == OrderStatus.RevisionSubmitted ||
                                         order.Status == OrderStatus.FinalSubmitted;
                    canAcceptWork = order.Status == OrderStatus.DraftSubmitted ||
                                    order.Status == OrderStatus.RevisionSubmitted ||
                                    order.Status == OrderStatus.FinalSubmitted;
                }

                var viewModel = new OrderWorkspaceViewModel
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    Title = order.Title,
                    Description = order.Description,
                    Subject = order.Subject,
                    AcademicLevel = order.AcademicLevel,
                    AcademicLevelName = breakdown.AcademicLevelName,
                    CitationFormat = order.CitationFormat,
                    CitationFormatName = order.CitationFormat.ToDisplayName(),
                    Deadline = order.Deadline,
                    Pages = order.Pages,
                    WordCount = order.WordCount,
                    Budget = order.Budget,
                    Status = order.Status,
                    IsAssigned = order.AssignedWriterId != null,
                    OtherPartyLabel = otherPartyLabel,
                    OtherPartyName = otherPartyName,
                    MyRole = myRole,
                    ConversationId = conversation?.Id,
                    Attachments = order.Attachments.ToList(),
                    Submissions = submissions,
                    RevisionRequests = revisions,
                    CanSubmitWork = canSubmitWork,
                    CanRequestRevision = canRequestRevision,
                    CanAcceptWork = canAcceptWork
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading workspace for order {OrderId}.", id);
                TempData["ErrorMessage"] = "An error occurred while loading the workspace.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// POST: /Orders/Workspace/{id}/SubmitWork
        /// Assigned writer uploads a draft/revision submission.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitWork(int id, IFormFile file, string? comments, SubmissionType submissionType = SubmissionType.Draft)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (order.AssignedWriterId != currentUser.Id && !User.IsInRole(RoleNames.Administrator))
                    return Forbid();

                await _workDeliveryService.SubmitWorkAsync(id, currentUser.Id, file, comments ?? string.Empty, submissionType);

                TempData["SuccessMessage"] = $"{submissionType} submitted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting work for order {OrderId}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Workspace), new { id });
        }

        /// <summary>
        /// POST: /Orders/Workspace/{id}/RequestRevision
        /// Client requests a revision with required message.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRevision(int id, string comments)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (string.IsNullOrWhiteSpace(comments) || comments.Length < 10)
                {
                    TempData["ErrorMessage"] = "Please provide detailed revision instructions (min 10 characters).";
                    return RedirectToAction(nameof(Workspace), new { id });
                }

                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (order.ClientId != currentUser.Id && !User.IsInRole(RoleNames.Administrator))
                    return Forbid();

                if (User.IsInRole(RoleNames.Administrator))
                {
                    await _workDeliveryService.AdminForceRevisionAsync(id, currentUser.Id, comments);
                    TempData["SuccessMessage"] = "Revision requested by admin.";
                }
                else
                {
                    await _workDeliveryService.RequestRevisionAsync(id, currentUser.Id, comments);
                    TempData["SuccessMessage"] = "Revision requested. The writer has been notified.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting revision for order {OrderId}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Workspace), new { id });
        }

        /// <summary>
        /// POST: /Orders/Workspace/{id}/AcceptWork
        /// Client marks work as accepted (completes the order without payment release).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptWork(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (order.ClientId != currentUser.Id && !User.IsInRole(RoleNames.Administrator))
                    return Forbid();

                // Simple acceptance: update status to Completed, no payment release
                order.Status = OrderStatus.Completed;
                order.UpdatedAt = DateTime.UtcNow;
                order.CompletedAt = DateTime.UtcNow;
                order.IsMarketplaceOpen = false;

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Work Accepted",
                    PerformedById = currentUser.Id,
                    TargetUserId = order.AssignedWriterId,
                    Description = $"Client accepted work for order {order.OrderNumber}.",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    order.AssignedWriterId!,
                    "Work Accepted",
                    $"Your work for order {order.OrderNumber} has been accepted.",
                    NotificationType.OrderCompleted,
                    order.Id.ToString());

                TempData["SuccessMessage"] = "Work accepted. Order has been marked as completed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting work for order {OrderId}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Workspace), new { id });
        }

        /// <summary>
        /// GET: /Orders/DownloadSubmission/{submissionId}
        /// Securely download a submission file. Access: order owner client, assigned writer, admin.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadSubmission(int submissionId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var submission = await _context.Set<OrderSubmission>()
                    .Include(s => s.Order)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null) return NotFound();

                var order = submission.Order;
                bool isAdmin = User.IsInRole(RoleNames.Administrator);
                bool isClient = order.ClientId == currentUser.Id;
                bool isAssignedWriter = order.AssignedWriterId == currentUser.Id;

                if (!isAdmin && !isClient && !isAssignedWriter)
                    return Forbid();

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "The requested file could not be found on the server.";
                    return RedirectToAction(nameof(Workspace), new { id = order.Id });
                }

                var contentType = GetContentType(submission.FileName);
                return PhysicalFile(filePath, contentType, submission.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading submission {SubmissionId}.", submissionId);
                TempData["ErrorMessage"] = "An error occurred while downloading the file.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Compatibility redirect: /Orders/Dashboard → /Dashboard
        /// </summary>
        [HttpGet]
        [Obsolete("Use /Dashboard instead.")]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Index", "Dashboard", new { area = "" });
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".zip" => "application/zip",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }
}

