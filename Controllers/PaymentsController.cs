using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;
using ScholarRescue.Services;
using ScholarRescue.Services.Payments;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller for processing order payments via Paystack.
    /// Orders must be paid before becoming available to writers.
    /// Funds are held in escrow until work is completed and approved.
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class PaymentsController : Controller
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaystackPaymentService _paystackService;
        private readonly IEscrowService _escrowService;
        private readonly IConfigurationService _configurationService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            IPaystackPaymentService paystackService,
            IEscrowService escrowService,
            IConfigurationService configurationService,
            INotificationService notificationService,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _paystackService = paystackService;
            _escrowService = escrowService;
            _configurationService = configurationService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Payments/Checkout/{orderId}
        /// Displays Paystack payment checkout page.
        /// </summary>
        [HttpGet("Checkout/{orderId:int}")]
        public async Task<IActionResult> Checkout(int orderId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == currentUser.Id);

                if (order == null) return NotFound();

                // Accept orders that are PendingPayment (Pay Now) or Open with PaymentDeferred (Pay Later)
                bool canPay = (order.Status == OrderStatus.PendingPayment) ||
                             (order.Status == OrderStatus.Open && order.PaymentDeferred);

                if (!canPay)
                {
                    TempData["ErrorMessage"] = "This order cannot be processed for payment at this time.";
                    return RedirectToAction("Index", "Orders");
                }

                ViewBag.PaystackPublicKey = _paystackService.GetPublicKey();
                ViewBag.CurrentProvider = PaymentProvider.Paystack;
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout for order {OrderId}.", orderId);
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("Index", "Orders");
            }
        }

        /// <summary>
        /// POST: /Payments/InitiatePaystack
        /// Initiates a Paystack transaction and redirects to Paystack checkout.
        /// </summary>
        [HttpPost("InitiatePaystack")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePaystack(int orderId, PaymentProvider provider = PaymentProvider.Paystack)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Challenge();

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == currentUser.Id);

                if (order == null) return NotFound();

                // Allow payment for PendingPayment orders (Pay Now) or Open+PaymentDeferred orders (Pay Later)
                bool canPay = (order.PaymentStatus == OrderPaymentStatus.PendingPayment) ||
                             (order.Status == OrderStatus.Open && order.PaymentDeferred);

                if (!canPay)
                {
                    TempData["ErrorMessage"] = "This order has already been processed.";
                    return RedirectToAction("Index", "Orders");
                }

                // Initialize Paystack transaction
                var (success, authUrl, accessCode, reference) = await _paystackService.InitializeTransactionAsync(
                    orderId, currentUser.Id, currentUser.Email ?? "");

                if (!success || string.IsNullOrEmpty(authUrl))
                {
                    _logger.LogError("Failed to initialize Paystack payment for Order {OrderId}", orderId);
                    TempData["ErrorMessage"] = "Failed to initialize payment. Please try again.";
                    return RedirectToAction("Checkout", new { orderId });
                }

                _logger.LogInformation("Paystack payment initialized for Order {OrderId}, Ref: {Ref}", orderId, reference);

                // Redirect to Paystack checkout
                return Redirect(authUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Paystack payment for order {OrderId}.", orderId);
                TempData["ErrorMessage"] = "An error occurred while processing payment.";
                return RedirectToAction("Checkout", new { orderId });
            }
        }

        /// <summary>
        /// GET: /Payments/PaystackCallback
        /// Handles the Paystack redirect callback after payment.
        /// Verifies transaction and funds escrow.
        /// </summary>
        [HttpGet("PaystackCallback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackCallback(string reference, int orderId)
        {
            if (string.IsNullOrEmpty(reference))
            {
                TempData["ErrorMessage"] = "Payment verification failed: No reference provided.";
                return RedirectToAction("Index", "Orders");
            }

            try
            {
                // Verify transaction with Paystack API
                var (success, amount, status, _) = await _paystackService.VerifyTransactionAsync(reference);

                if (!success || status != "success")
                {
                    _logger.LogWarning("Paystack payment verification failed for Ref: {Ref}, Status: {Status}", reference, status);
                    TempData["ErrorMessage"] = $"Payment verification failed. Status: {status}";
                    return RedirectToAction("Details", "Orders", new { id = orderId });
                }

                var order = await _context.Orders
                    .Include(o => o.Client)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Index", "Orders");
                }

                // Prevent double processing
                if (order.PaymentStatus != OrderPaymentStatus.PendingPayment)
                {
                    TempData["SuccessMessage"] = "Payment already processed successfully.";
                    return RedirectToAction("Details", "Orders", new { id = orderId });
                }

                var commissionRate = await _configurationService.GetCommissionRateAsync();
                var commission = Math.Round(amount * commissionRate, 2);
                var writerEarnings = amount - commission;

                // Mark payment as paid
                order.PaymentStatus = OrderPaymentStatus.Paid;
                order.PaymentDeferred = false;
                order.Status = OrderStatus.Open;
                order.PaymentDate = DateTime.UtcNow;
                order.EscrowFundedDate = DateTime.UtcNow;
                order.IsMarketplaceOpen = true;
                order.CommissionAmount = commission;
                order.WriterEarnings = writerEarnings;
                order.PaystackReference = reference;

                // Create payment record
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = amount,
                    PaymentMethod = "Paystack",
                    Status = PaymentStatus.Completed,
                    TransactionReference = reference,
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
                _context.Payments.Add(payment);

                // Create escrow record
                var escrow = new EscrowAccount
                {
                    OrderId = order.Id,
                    ClientId = order.ClientId,
                    TotalAmount = amount,
                    FundedAmount = amount,
                    Status = EscrowStatus.Funded,
                    CreatedAt = DateTime.UtcNow
                };
                _context.EscrowAccounts.Add(escrow);

                // Create ledger entry - ClientFunding + Escrow
                var fundingTx = new FinancialTransaction
                {
                    TransactionNumber = $"PS-{reference[..8].ToUpper()}-{order.Id}",
                    UserId = order.ClientId,
                    TransactionType = TransactionType.OrderFunded,
                    Description = $"Client funded escrow for Order #{order.OrderNumber} via Paystack",
                    CreditAmount = amount,
                    BalanceAfter = amount,
                    ReferenceType = "Order",
                    ReferenceId = order.Id,
                    CreatedBy = order.ClientId,
                    CreatedDate = DateTime.UtcNow
                };
                _context.FinancialTransactions.Add(fundingTx);

                // Order history
                _context.OrderHistories.Add(new OrderHistory
                {
                    OrderId = order.Id,
                    OldStatus = OrderStatus.PendingPayment,
                    NewStatus = OrderStatus.Funded,
                    ChangedById = order.ClientId,
                    CreatedAt = DateTime.UtcNow,
                    Notes = $"Payment received via Paystack. Reference: {reference}"
                });

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Paystack Payment Completed",
                    PerformedById = order.ClientId,
                    Description = $"Paystack payment of ${amount:F2} for Order #{order.OrderNumber}. Ref: {reference}. Escrow funded.",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Notifications
                await _notificationService.CreateNotificationAsync(
                    order.ClientId,
                    "Payment Successful – Escrow Funded",
                    $"Your payment of ${amount:F2} for Order #{order.OrderNumber} has been received successfully. Your funds are securely held in escrow until the assigned tutor successfully completes your work.",
                    NotificationType.OrderFunded, order.Id.ToString(), "Order");

                var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Administrator);
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id, "New Payment Received",
                        $"Payment of ${amount:F2} received for Order #{order.OrderNumber} via Paystack.",
                        NotificationType.OrderFunded, order.Id.ToString(), "Order");
                }

                _logger.LogInformation("Paystack payment completed. Order {OrderId}, Ref: {Ref}, Amount: {Amount}",
                    orderId, reference, amount);

                TempData["SuccessMessage"] = $"Payment of ${amount:F2} successful! Your funds are now held securely in escrow. Writers can now apply to your order.";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Paystack callback for Ref: {Ref}", reference);
                TempData["ErrorMessage"] = "An error occurred during payment verification.";
                return RedirectToAction("Details", "Orders", new { id = orderId });
            }
        }

        /// <summary>
        /// POST: /Payments/PaystackWebhook
        /// Handles incoming Paystack webhook events with signature validation.
        /// </summary>
        [HttpPost("PaystackWebhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackWebhook()
        {
            string payload;
            using (var reader = new StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault() ?? "";

            await _paystackService.ProcessWebhookAsync(payload, signature);

            return Ok();
        }
    }
}
