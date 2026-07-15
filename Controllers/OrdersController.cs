using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;
using ScholarRescue.Services;
using System.Text;
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
        private readonly IOrderAttachmentService _orderAttachmentService;
        private readonly IEscrowService _escrowService;

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
            INotificationService notificationService,
            IOrderAttachmentService orderAttachmentService,
            IEscrowService escrowService)
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
            _orderAttachmentService = orderAttachmentService;
            _escrowService = escrowService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                IQueryable<TutoringRequest> ordersQuery = _context.Orders
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
                    order.AcademicLevel, order.Pages ?? 1, order.Deadline);

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
                    CompletedAt = order.CompletedAt,
                    PaymentDeferred = order.PaymentDeferred
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

        // Server-side validation: ensure draft attachment exists for types that require it
        if (viewModel.RequestType == RequestType.DraftFeedback ||
            viewModel.RequestType == RequestType.ProofreadingOwnWork)
        {
            if (viewModel.UploadedFileData == null || viewModel.UploadedFileData.Count == 0)
            {
                ModelState.AddModelError(nameof(viewModel.UploadedFileData),
                    "Please upload the work you'd like feedback on before submitting this request.");
                return View(viewModel);
            }
        }

        // Validate actual files before any database writes
        if (viewModel.UploadedFileData != null && viewModel.UploadedFileData.Count > 0)
        {
            try
            {
                _orderAttachmentService.ValidateFiles(viewModel.UploadedFileData);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning("File validation failed for order creation: {Message}", ex.Message);
                return View(viewModel);
            }
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
            int effectivePages = viewModel.Pages ?? 1;
            var wordCount = _pricingService.CalculateWordCount(effectivePages);
            var budget = _pricingService.CalculatePrice(
                viewModel.AcademicLevel, effectivePages, viewModel.Deadline);
            var commissionRate = await _configurationService.GetCommissionRateAsync();
            var commission = Math.Round(budget * commissionRate, 2);
            var writerEarnings = budget - commission;

                var order = new TutoringRequest
                {
                    OrderNumber = orderNumber,
                    RequestType = viewModel.RequestType,
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    Subject = subject,
                    AcademicLevel = viewModel.AcademicLevel,
                    CitationFormat = viewModel.CitationFormat,
                    Deadline = viewModel.Deadline,
                    Pages = viewModel.Pages,
                    WordCount = viewModel.Pages.HasValue ? wordCount : null,
                    Budget = budget,
                    CommissionAmount = commission,
                    WriterEarnings = writerEarnings,
                    NumberOfSources = viewModel.NumberOfSources,
                    Priority = PriorityLevel.Normal,
                    Status = OrderStatus.PendingPayment,
                    ClientId = currentUser.Id,
                    IsMarketplaceOpen = false,
                    PaymentDeferred = viewModel.PayLater,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (viewModel.PayLater)
            {
                // Pay Later: create escrow record, open order to marketplace immediately
                await _escrowService.CreateEscrowAsync(order.Id, currentUser.Id);

                order.Status = OrderStatus.Open;
                order.IsMarketplaceOpen = true;
                order.PaymentDeferred = true;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    OldStatus = OrderStatus.PendingPayment,
                    NewStatus = OrderStatus.Open,
                    ChangedById = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Order created with Pay Later — posted to marketplace immediately"
                });

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Order Created (Pay Later)",
                    PerformedById = currentUser.Id,
                    TargetUserId = currentUser.Id,
                    Description = $"Order {orderNumber} created with Pay Later, escrow created, posted to marketplace",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Save uploaded attachments (if any)
                if (viewModel.UploadedFileData != null && viewModel.UploadedFileData.Count > 0)
                {
                    var purpose = (viewModel.RequestType == RequestType.DraftFeedback ||
                                   viewModel.RequestType == RequestType.ProofreadingOwnWork)
                        ? AttachmentPurpose.StudentDraft
                        : AttachmentPurpose.SupportingMaterial;

                    await _orderAttachmentService.SaveAttachmentsAsync(
                        order.Id, viewModel.UploadedFileData, purpose, currentUser.Id);
                }

                _logger.LogInformation("Order {OrderNumber} created with Pay Later, redirecting to details.", orderNumber);

                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            else
            {
                // Pay Now (current behavior unchanged)
                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    OldStatus = OrderStatus.PendingPayment,
                    NewStatus = OrderStatus.PendingPayment,
                    ChangedById = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    Notes = "Order created, pending payment"
                });

                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Order Created (Pending Payment)",
                    PerformedById = currentUser.Id,
                    TargetUserId = currentUser.Id,
                    Description = $"Order {orderNumber} created, pending payment",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Save uploaded attachments (if any)
                if (viewModel.UploadedFileData != null && viewModel.UploadedFileData.Count > 0)
                {
                    var purpose = (viewModel.RequestType == RequestType.DraftFeedback ||
                                   viewModel.RequestType == RequestType.ProofreadingOwnWork)
                        ? AttachmentPurpose.StudentDraft
                        : AttachmentPurpose.SupportingMaterial;

                    await _orderAttachmentService.SaveAttachmentsAsync(
                        order.Id, viewModel.UploadedFileData, purpose, currentUser.Id);
                }

                _logger.LogInformation("Order {OrderNumber} created, redirecting to payment.", orderNumber);

                return RedirectToAction("Checkout", "Payments", new { orderId = order.Id });
            }
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
                    RequestType = order.RequestType,
                    IsRequestTypeLocked = order.Status != OrderStatus.Draft,
                    Title = order.Title,
                    Description = order.Description,
                    Subject = order.Subject,
                    AcademicLevel = order.AcademicLevel,
                    CitationFormat = order.CitationFormat,
                    Deadline = order.Deadline,
                    Pages = order.Pages,
                    WordCount = order.WordCount,
                    Budget = order.Budget,
                    IsPagesLocked = order.Status != OrderStatus.Draft,
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

                // Always-editable fields
                order.Title = viewModel.Title;
                order.Description = viewModel.Description;
                order.Subject = viewModel.Subject;
                order.AcademicLevel = viewModel.AcademicLevel;
                order.CitationFormat = viewModel.CitationFormat;
                order.NumberOfSources = viewModel.NumberOfSources;

                // Lock Pages/Deadline once order leaves Draft — re-read from DB, ignore form
                if (order.Status == OrderStatus.Draft)
                {
                    order.Pages = viewModel.Pages;
                    order.Deadline = viewModel.Deadline;
                    int editPages = viewModel.Pages ?? 1;
                    order.WordCount = _pricingService.CalculateWordCount(editPages);
                    order.Budget = _pricingService.CalculatePrice(viewModel.AcademicLevel, editPages, viewModel.Deadline);
                    var commissionRateForEdit = await _configurationService.GetCommissionRateAsync();
                    order.CommissionAmount = Math.Round(order.Budget * commissionRateForEdit, 2);
                    order.WriterEarnings = order.Budget - order.CommissionAmount;
                }

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
        /// This page is intended ONLY for anonymous visitors.
        /// - Anonymous visitors: allowed.
        /// - Authenticated Clients: redirected to Orders/Create.
        /// - Authenticated Writers/Tutors: redirected to their dashboard with an info message.
        /// - Authenticated Administrators: redirected to Orders/Create.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GuestCreate()
        {
            // Any authenticated user (Client, Writer, Administrator) must be redirected
            // away from the guest flow. The guest endpoint is strictly for anonymous visitors.
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(RoleNames.Writer))
                {
                    TempData["OrderNowMessage"] = "Tutors cannot submit support requests.";
                    TempData["OrderNowIsError"] = "true";
                    return RedirectToAction("Dashboard", "Writers");
                }

                // Clients and Administrators → the authenticated create-request form.
                return RedirectToAction("Create", "Orders");
            }

            return View(new GuestOrderViewModel
            {
                Deadline = DateTime.UtcNow.AddDays(7)
            });
        }

        /// <summary>
        /// Handle guest order submission: create the client account, create the request,
        /// persist any uploaded files, then sign the new client in.
        /// This POST endpoint strictly rejects all authenticated users.
        ///
        /// TRANSACTION STRATEGY
        /// ─────────────────────
        /// ScholarRescueDbContext extends IdentityDbContext, so UserManager and the
        /// controller's _context share the SAME scoped DbContext instance (registered
        /// via AddDbContext + AddIdentity → AddEntityFrameworkStores in Program.cs).
        /// The explicit transaction opened on _context.Database covers Identity writes
        /// (user creation, role assignment) and business writes (order, history, audit,
        /// escrow) atomically — they all go through the same connection and transaction.
        ///
        /// File operations (attachment persistence to disk + DB records) happen after
        /// the transaction commits. If they fail, compensating cleanup removes the
        /// request and user so no orphaned data is left behind.
        ///
        /// The user is NOT signed in until EVERYTHING — account, request, AND files —
        /// has been persisted successfully.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuestCreate(GuestOrderViewModel model)
        {
            // ── Direct endpoint guard: reject ALL authenticated users ──
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(RoleNames.Writer))
                {
                    TempData["OrderNowMessage"] = "Tutors cannot submit support requests.";
                    TempData["OrderNowIsError"] = "true";
                    return RedirectToAction("Dashboard", "Writers");
                }

                // Clients and Administrators → the authenticated create-request form.
                return RedirectToAction("Create", "Orders");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Enforce draft upload for request types that require it.
            if (model.RequestType == RequestType.DraftFeedback ||
                model.RequestType == RequestType.ProofreadingOwnWork)
            {
                if (model.UploadedFileData == null || model.UploadedFileData.Count == 0)
                {
                    ModelState.AddModelError(nameof(model.UploadedFileData),
                        "Please upload the work you'd like feedback on before submitting this request.");
                    return View(model);
                }
            }

            // Validate actual files before any database writes (account / order).
            if (model.UploadedFileData != null && model.UploadedFileData.Count > 0)
            {
                try
                {
                    _orderAttachmentService.ValidateFiles(model.UploadedFileData);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    _logger.LogWarning("Guest file validation failed: {Message}", ex.Message);
                    return View(model);
                }
            }

            // Check for duplicate email BEFORE writing anything. Do not reveal the
            // existing account's role (client / tutor / administrator).
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(model.Email),
                    "An account already exists for this email. Please sign in to continue.");
                return View(model);
            }

            // ── Single transaction: UserManager + role + request + history + audit ──
            // Both UserManager and _context share the same scoped ScholarRescueDbContext
            // (IdentityDbContext<ApplicationUser>), so the connection-level transaction
            // created by BeginTransactionAsync covers ALL writes — Identity and business.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            ApplicationUser? createdUser = null;
            TutoringRequest? createdOrder = null;

            try
            {
                // 1. Create the client account via the existing Identity/UserManager flow.
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserType = RoleNames.Client,
                    IsActive = true,
                    RegistrationCompleted = true,
                    RegistrationSource = "OrderFlow",
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    foreach (var err in createResult.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);
                    return View(model);
                }

                createdUser = user;

                // Assign the existing Client role using the project role constant.
                var roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Client);
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("Role assignment failed for guest user {Email}: {Errors}",
                        model.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    ModelState.AddModelError(string.Empty, "An error occurred while setting up your account. Please try again.");
                    return View(model);
                }

                // 2. Create the request linked to the newly created client.
                var lastOrder = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
                int nextNumber = (lastOrder?.Id ?? 0) + 1;
                string orderNumber = $"SR-{DateTime.UtcNow.Year}-{nextNumber:D6}";
                int guestPages = model.Pages ?? 1;
                var wordCount = _pricingService.CalculateWordCount(guestPages);
                var budget = _pricingService.CalculatePrice(model.AcademicLevel, guestPages, model.Deadline);
                var commissionRate = await _configurationService.GetCommissionRateAsync();
                var commission = Math.Round(budget * commissionRate, 2);
                var writerEarnings = budget - commission;

                var order = new TutoringRequest
                {
                    OrderNumber = orderNumber,
                    RequestType = model.RequestType,
                    Title = model.Title,
                    Description = model.Description,
                    Subject = model.Subject,
                    AcademicLevel = model.AcademicLevel,
                    CitationFormat = model.CitationFormat,
                    Deadline = model.Deadline,
                    Pages = model.Pages,
                    WordCount = model.Pages.HasValue ? wordCount : null,
                    Budget = budget,
                    CommissionAmount = commission,
                    WriterEarnings = writerEarnings,
                    NumberOfSources = model.NumberOfSources,
                    Priority = PriorityLevel.Normal,
                    Status = OrderStatus.PendingPayment,
                    ClientId = user.Id,
                    IsMarketplaceOpen = false,
                    PaymentDeferred = model.PayLater,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                createdOrder = order;

                // Pay Later: create escrow and open the request to the marketplace immediately.
                if (model.PayLater)
                {
                    await _escrowService.CreateEscrowAsync(order.Id, user.Id);

                    order.Status = OrderStatus.Open;
                    order.IsMarketplaceOpen = true;
                    order.PaymentDeferred = true;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _context.OrderHistories.Add(new OrderHistory
                    {
                        OrderId = order.Id,
                        OldStatus = OrderStatus.PendingPayment,
                        NewStatus = OrderStatus.Open,
                        ChangedById = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        Notes = "Order created via guest flow with Pay Later — posted to marketplace immediately"
                    });

                    _context.AuditLogs.Add(new AuditLog
                    {
                        Action = "Order Created (Guest Pay Later)",
                        PerformedById = user.Id,
                        TargetUserId = user.Id,
                        Description = $"Client registered through order flow. Order {orderNumber} created with Pay Later.",
                        CreatedDate = DateTime.UtcNow
                    });
                }
                else
                {
                    _context.OrderHistories.Add(new OrderHistory
                    {
                        OrderId = order.Id,
                        OldStatus = OrderStatus.PendingPayment,
                        NewStatus = OrderStatus.PendingPayment,
                        ChangedById = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        Notes = "Order created via guest flow"
                    });

                    _context.AuditLogs.Add(new AuditLog
                    {
                        Action = "Order Created",
                        PerformedById = user.Id,
                        TargetUserId = user.Id,
                        Description = $"Client registered through order flow. Order {orderNumber} created.",
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                // Commit user + role + request + history + audit atomically.
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during guest account/order transaction for {Email}. Rolled back.", model.Email);

                // If UserManager succeeded (user was created) but something later failed,
                // the rollback undoes all changes including the Identity user record since
                // they share the same context+connection+transaction.
                ModelState.AddModelError(string.Empty, "An error occurred while creating your account and request. Please try again.");
                return View(model);
            }

            // ── Post-commit: persist uploaded files (outside the transaction) ──
            if (model.UploadedFileData != null && model.UploadedFileData.Count > 0 && createdOrder != null)
            {
                try
                {
                    var purpose = (model.RequestType == RequestType.DraftFeedback ||
                                   model.RequestType == RequestType.ProofreadingOwnWork)
                        ? AttachmentPurpose.StudentDraft
                        : AttachmentPurpose.SupportingMaterial;

                    await _orderAttachmentService.SaveAttachmentsAsync(
                        createdOrder.Id, model.UploadedFileData, purpose, createdUser!.Id);
                }
                catch (Exception ex)
                {
                    // Compensating cleanup: remove the request and the account we just created,
                    // and delete any files that were written, so we never leave an orphaned
                    // client account or request behind.
                    _logger.LogError(ex, "File upload failed for guest order {OrderNumber}. Compensating cleanup.", createdOrder.OrderNumber);
                    await CompensateGuestCreationAsync(createdOrder, createdUser!);
                    ModelState.AddModelError(string.Empty, "An error occurred while saving your files. Your account and request have been removed. Please try again.");
                    return View(model);
                }
            }

            // ── Everything succeeded: sign the new client in and redirect ──
            await _signInManager.SignInAsync(createdUser!, isPersistent: true);

            // Send the client welcome email (non-blocking — never fails the request).
            try
            {
                await _verificationService.SendClientWelcomeEmailAsync(model.Email, $"{model.FirstName} {model.LastName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to guest client {Email}.", model.Email);
            }

            _logger.LogInformation("Guest user {Email} created account and order {OrderNumber}", model.Email, createdOrder!.OrderNumber);

            if (model.PayLater)
            {
                TempData["SuccessMessage"] = "Account created and order posted to marketplace! Writers can now view and apply.";
                return RedirectToAction(nameof(Details), new { id = createdOrder.Id });
            }

            TempData["SuccessMessage"] = "Account created and order placed! Proceed to payment.";
            return RedirectToAction("Checkout", "Payments", new { orderId = createdOrder.Id });
        }

        /// <summary>
        /// Compensating cleanup for a failed guest creation workflow.
        /// Removes any files uploaded to disk, then deletes the request and user
        /// that were created during this guest submission. Only affects accounts
        /// and requests created in the current failed guest flow — never touches
        /// pre-existing data.
        ///
        /// Each step is individually wrapped in try/catch so that a failure to clean
        /// up one resource does not prevent cleanup of the remaining resources.
        /// </summary>
        private async Task CompensateGuestCreationAsync(TutoringRequest order, ApplicationUser user)
        {
            // 1. Remove any files already written to disk for this order.
            try
            {
                var uploadDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot", "uploads", "orders", order.Id.ToString(), "attachments");
                if (Directory.Exists(uploadDir))
                {
                    Directory.Delete(uploadDir, recursive: true);
                    _logger.LogInformation("Deleted upload directory for failed guest order {OrderId}.", order.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete upload directory during guest compensation for order {OrderId}.", order.Id);
            }

            // 2. Remove the request (cascade removes attachments, history, audit rows).
            try
            {
                var trackedOrder = await _context.Orders.FindAsync(order.Id);
                if (trackedOrder != null)
                {
                    _context.Orders.Remove(trackedOrder);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed guest order {OrderId} during compensation.", order.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove guest order {OrderId} during compensation.", order.Id);
            }

            // 3. Remove the user account created for this guest.
            //    Only deletes if the user was created during THIS submission —
            //    never touches a pre-existing account.
            try
            {
                var trackedUser = await _userManager.FindByIdAsync(user.Id);
                if (trackedUser != null)
                {
                    // Guard: only delete users with RegistrationSource == "OrderFlow"
                    // to never accidentally affect a pre-existing account.
                    if (trackedUser.RegistrationSource == "OrderFlow")
                    {
                        await _userManager.DeleteAsync(trackedUser);
                        _logger.LogInformation("Removed guest user {UserId} during compensation.", user.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Skipped user deletion during compensation: user {UserId} was not an OrderFlow registration.", user.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete guest user {UserId} during compensation.", user.Id);
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
                    order.AcademicLevel, order.Pages ?? 1, order.Deadline);

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
                    RequestType = order.RequestType,
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
                    CanAcceptWork = canAcceptWork,
                    PaymentStatus = order.PaymentStatus,
                    PaymentDeferred = order.PaymentDeferred
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
        public async Task<IActionResult> SubmitWork(int id, IFormFile file, string? comments, int? reviewedAttachmentId, SubmissionType submissionType = SubmissionType.Draft)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (order.AssignedWriterId != currentUser.Id && !User.IsInRole(RoleNames.Administrator))
                    return Forbid();

                await _workDeliveryService.SubmitWorkAsync(id, currentUser.Id, file, comments ?? string.Empty, submissionType, reviewedAttachmentId);

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

                // Payment gate: client can only download full file if order is paid.
                // Admins and assigned writers bypass this restriction.
                if (isClient && !isAdmin && !order.CanClientAccessFullSubmission)
                {
                    _logger.LogInformation("Client {UserId} requested download of unpaid submission {SubmissionId} for order {OrderId}. Serving preview/blocked.", currentUser.Id, submissionId, order.Id);

                    var ext = Path.GetExtension(submission.FileName).ToLowerInvariant();
                    if (ext == ".txt")
                    {
                        // Serve a truncated preview (first 25% of lines) with marker
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FilePath.TrimStart('/'));
                        string previewContent;
                        try
                        {
                            var allLines = await System.IO.File.ReadAllLinesAsync(fullPath);
                            var totalLines = allLines.Length;
                            var previewLineCount = Math.Max(1, (int)Math.Ceiling(totalLines * 0.25));
                            var previewLines = allLines.Take(previewLineCount).ToArray();
                            previewContent = string.Join(Environment.NewLine, previewLines)
                                + Environment.NewLine + Environment.NewLine
                                + "[... Preview truncated. Complete payment to view the full document ...]";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Could not read .txt file for preview generation: {FilePath}", fullPath);
                            TempData["ErrorMessage"] = "Payment is required before this file can be downloaded. Preview is not available for this file type.";
                            return RedirectToAction(nameof(Workspace), new { id = order.Id });
                        }

                        var previewBytes = Encoding.UTF8.GetBytes(previewContent);
                        return File(previewBytes, "text/plain", $"preview_{submission.FileName}");
                    }

                    // Non-.txt files: redirect with "payment required" message
                    TempData["ErrorMessage"] = "Payment is required before this file can be downloaded. Preview is not available for this file type.";
                    return RedirectToAction(nameof(Workspace), new { id = order.Id });
                }

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

