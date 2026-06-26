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
        private readonly SignInManager<ApplicationUser> _signInManager;

        public OrdersController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OrdersController> logger,
            IPricingService pricingService,
            IWalletService walletService,
            IVerificationService verificationService,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _pricingService = pricingService;
            _walletService = walletService;
            _verificationService = verificationService;
            _signInManager = signInManager;
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

                if (User.IsInRole("Administrator"))
                {
                    // Admins see all orders
                }
                else if (User.IsInRole("Client"))
                {
                    ordersQuery = ordersQuery.Where(o => o.ClientId == currentUser.Id);
                }
                else if (User.IsInRole("Writer"))
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

                if (!User.IsInRole("Administrator") &&
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
        [Authorize(Roles = "Client,Administrator")]
        public IActionResult Create()
        {
            return View(new CreateOrderViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client,Administrator")]
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
                var commission = budget * 0.10m;
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
        [Authorize(Roles = "Client,Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole("Administrator") && order.ClientId != currentUser.Id)
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
        [Authorize(Roles = "Client,Administrator")]
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

                if (!User.IsInRole("Administrator") && order.ClientId != currentUser.Id)
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
                order.CommissionAmount = order.Budget * 0.10m;
                order.WriterEarnings = order.Budget - order.CommissionAmount;
                order.UpdatedAt = DateTime.UtcNow;

                if (User.IsInRole("Administrator"))
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
        [Authorize(Roles = "Client,Administrator")]
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

                if (!User.IsInRole("Administrator") && order.ClientId != currentUser.Id)
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
        [Authorize(Roles = "Client,Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                if (!User.IsInRole("Administrator") && order.ClientId != currentUser.Id)
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
                    UserType = "Client",
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
                await _userManager.AddToRoleAsync(user, "Client");

                // 2. Create the order
                var lastOrder = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                int nextNumber = (lastOrder?.Id ?? 0) + 1;
                string orderNumber = $"SR-{DateTime.UtcNow.Year}-{nextNumber:D6}";
                var wordCount = _pricingService.CalculateWordCount(model.Pages);
                var budget = _pricingService.CalculatePrice(model.AcademicLevel, model.Pages, model.Deadline);
                var commission = budget * 0.10m;
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
        [Authorize(Roles = "Client,Administrator")]
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
                if (!User.IsInRole("Administrator") && order.ClientId != currentUser.Id)
                    return Forbid();

                bool isAdmin = User.IsInRole("Administrator");

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

        /// <summary>
        /// Client dashboard showing all orders including drafts.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var ordersQuery = _context.Orders
                    .Include(o => o.AssignedWriter)
                    .Where(o => o.ClientId == currentUser.Id)
                    .AsNoTracking();

                var totalOrders = await ordersQuery.CountAsync();
                var openOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Open || o.Status == OrderStatus.PendingReview);
                var inProgressOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Assigned || o.Status == OrderStatus.InProgress);
                var completedOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Completed);

                var recentOrders = await ordersQuery
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new OrderIndexViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Title = o.Title,
                        ClientName = currentUser.FirstName + " " + currentUser.LastName,
                        WriterName = o.AssignedWriter != null ? o.AssignedWriter.FirstName + " " + o.AssignedWriter.LastName : null,
                        Status = o.Status,
                        Deadline = o.Deadline,
                        Pages = o.Pages,
                        Budget = o.Budget,
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync();

                var dashboard = new ClientDashboardViewModel
                {
                    TotalOrders = totalOrders,
                    OpenOrders = openOrders,
                    InProgressOrders = inProgressOrders,
                    CompletedOrders = completedOrders,
                    RecentOrders = recentOrders
                };

                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client dashboard.");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}