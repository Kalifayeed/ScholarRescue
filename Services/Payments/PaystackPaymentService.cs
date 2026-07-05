using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using Microsoft.Extensions.Options;
using ScholarRescue.Models.Configuration;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Services.Payments
{
    /// <summary>
    /// Paystack Payment Gateway Service implementation.
    /// Handles transaction initialization, verification, webhooks, and ledger integration.
    /// </summary>
    public class PaystackPaymentService : IPaystackPaymentService
    {
        private readonly PaystackSettings _paystackSettings;
        private readonly CurrencySettings _currencySettings;
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaystackPaymentService> _logger;
        private readonly HttpClient _httpClient;

        private const string PaystackApiBase = "https://api.paystack.co";

        public PaystackPaymentService(
            IOptions<PaystackSettings> paystackOptions,
            IOptions<CurrencySettings> currencyOptions,
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<PaystackPaymentService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _paystackSettings = paystackOptions.Value;
            _currencySettings = currencyOptions.Value;
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("Paystack");
            _httpClient.BaseAddress = new Uri(PaystackApiBase);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _paystackSettings.SecretKey);
        }

        public string GetPublicKey() => _paystackSettings.PublicKey;

        /// <summary>
        /// Initializes a Paystack transaction for an order checkout.
        /// </summary>
        public async Task<(bool Success, string AuthorizationUrl, string AccessCode, string Reference)> InitializeTransactionAsync(
            int orderId, string userId, string userEmail)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ClientId == userId);

            if (order == null)
                return (false, string.Empty, string.Empty, string.Empty);

            if (order.Budget <= 0)
                return (false, string.Empty, string.Empty, string.Empty);

            if (order.PaymentStatus != OrderPaymentStatus.PendingPayment)
                return (false, string.Empty, string.Empty, string.Empty);

            // Generate unique transaction reference
            var reference = $"SR-{order.OrderNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}".ToUpper();

            try
            {
                var amountInKobo = (long)(order.Budget * 100); // Paystack uses kobo (cents)
                var baseAddress = _httpClient.BaseAddress?.AbsoluteUri ?? throw new InvalidOperationException("Paystack HttpClient BaseAddress is not configured.");
                var callbackUrl = $"{baseAddress.Replace(PaystackApiBase, string.Empty)}/Payments/PaystackCallback?reference={reference}&orderId={orderId}";

                var payload = new
                {
                    email = userEmail,
                    amount = amountInKobo,
                    currency = _currencySettings.CurrencyCode,
                    reference = reference,
                    callback_url = $"{GetBaseUrl()}/Payments/PaystackCallback?orderId={orderId}",
                    metadata = new
                    {
                        order_id = orderId,
                        order_number = order.OrderNumber,
                        user_id = userId,
                        platform_currency = _currencySettings.CurrencyCode
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/transaction/initialize", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Paystack init failed: {Status} {Body}", response.StatusCode, responseBody);
                    return (false, string.Empty, string.Empty, reference);
                }

                var result = JsonSerializer.Deserialize<PaystackInitResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.status == true && result.data != null)
                {
                    // Store reference on order
                    order.PaystackReference = result.data.reference;
                    order.PaystackAccessCode = result.data.access_code;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Paystack transaction initialized: {Ref} for Order {OrderId}",
                        result.data.reference, orderId);

                    return (true, result.data.authorization_url, result.data.access_code, result.data.reference);
                }

                _logger.LogWarning("Paystack init returned false status: {Body}", responseBody);
                return (false, string.Empty, string.Empty, reference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack initialization failed for Order {OrderId}", orderId);
                return (false, string.Empty, string.Empty, reference);
            }
        }

        /// <summary>
        /// Verifies a Paystack transaction using the reference.
        /// Returns amount in original currency (dollars).
        /// </summary>
        public async Task<(bool Success, decimal Amount, string Status, string GatewayResponse)> VerifyTransactionAsync(string reference)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/transaction/verify/{Uri.EscapeDataString(reference)}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Paystack verify failed: {Status} {Body}", response.StatusCode, responseBody);
                    return (false, 0, "failed", responseBody);
                }

                var result = JsonSerializer.Deserialize<PaystackVerifyResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.status == true && result.data != null)
                {
                    var amountInDollars = result.data.amount / 100m; // Convert from kobo
                    var gatewayStatus = result.data.status ?? "unknown";

                    _logger.LogInformation("Paystack verification: Ref={Ref} Status={Status} Amount={Amount}",
                        reference, gatewayStatus, amountInDollars);

                    return (gatewayStatus == "success", amountInDollars, gatewayStatus, responseBody);
                }

                return (false, 0, "verification_failed", responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack verification failed for reference {Ref}", reference);
                return (false, 0, "error", ex.Message);
            }
        }

        /// <summary>
        /// Processes an incoming Paystack webhook event.
        /// Validates signature, prevents duplicates, idempotent.
        /// </summary>
        public async Task ProcessWebhookAsync(string payload, string signature)
        {
            try
            {
                // Validate signature
                var computedHash = ComputeHash(payload, _paystackSettings.WebhookSecret);
                if (computedHash != signature)
                {
                    _logger.LogWarning("Paystack webhook: Invalid signature");
                    return;
                }

                var webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (webhookEvent == null)
                {
                    _logger.LogWarning("Paystack webhook: Unable to parse event");
                    return;
                }

                _logger.LogInformation("Paystack webhook received: Event={Event}, Ref={Ref}",
                    webhookEvent.@event, webhookEvent.data?.reference);

                // Only process charge.success events
                if (webhookEvent.@event != "charge.success")
                {
                    _logger.LogInformation("Paystack webhook: Ignoring event {Event}", webhookEvent.@event);
                    return;
                }

                var reference = webhookEvent.data?.reference;
                if (string.IsNullOrEmpty(reference)) return;

                // Prevent duplicate processing (idempotent)
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionReference == reference);

                if (existingPayment != null && existingPayment.Status == PaymentStatus.Completed)
                {
                    _logger.LogInformation("Paystack webhook: Duplicate event for ref {Ref}, skipping", reference);
                    return;
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.PaystackReference == reference);

                if (order == null)
                {
                    _logger.LogWarning("Paystack webhook: No order found for reference {Ref}", reference);
                    return;
                }

                // Prevent double processing
                if (order.PaymentStatus != OrderPaymentStatus.PendingPayment)
                {
                    _logger.LogInformation("Paystack webhook: Order {OrderId} already processed", order.Id);
                    return;
                }

                var amountInKobo = webhookEvent.data?.amount ?? 0;
                var amount = amountInKobo / 100m;

                // Mark payment as paid
                order.PaymentStatus = OrderPaymentStatus.Paid;
                order.PaymentDeferred = false;
                order.Status = OrderStatus.Open;
                order.PaymentDate = DateTime.UtcNow;
                order.EscrowFundedDate = DateTime.UtcNow;
                order.IsMarketplaceOpen = true;

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

                // Create ledger entry - ClientFunding
                var clientFundingTx = new FinancialTransaction
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
                _context.FinancialTransactions.Add(clientFundingTx);

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
                    Action = "Payment Completed",
                    PerformedById = order.ClientId,
                    Description = $"Paystack payment of ${amount:F2} for Order #{order.OrderNumber}. Ref: {reference}. Escrow funded.",
                    CreatedDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Notifications
                await _notificationService.CreateNotificationAsync(
                    order.ClientId,
                    "Payment Successful",
                    $"Your payment of ${amount:F2} for Order #{order.OrderNumber} has been received. Funds are held securely in escrow.",
                    NotificationType.OrderFunded, order.Id.ToString(), "Order");

                var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Administrator);
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id, "New Payment Received",
                        $"Payment of ${amount:F2} received for Order #{order.OrderNumber} via Paystack. Escrow funded.",
                        NotificationType.OrderFunded, order.Id.ToString(), "Order");
                }

                _logger.LogInformation("Paystack payment processed successfully. Order {OrderId}, Ref: {Ref}, Amount: {Amount}",
                    order.Id, reference, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack webhook processing failed");
            }
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/balance");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Paystack API connection successful");
                }

                return (false, $"Paystack API error: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack connection test failed");
                return (false, $"Paystack connection failed: {ex.Message}");
            }
        }

        #region Helpers

        private string GetBaseUrl()
        {
            // In production, this should be configurable
            return "https://localhost:5001";
        }

        private static string ComputeHash(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return hex;
        }

        #endregion

        #region Paystack DTOs

        private class PaystackInitResponse
        {
            public bool status { get; set; }
            public string? message { get; set; }
            public PaystackInitData? data { get; set; }
        }

        private class PaystackInitData
        {
            public string authorization_url { get; set; } = string.Empty;
            public string access_code { get; set; } = string.Empty;
            public string reference { get; set; } = string.Empty;
        }

        private class PaystackVerifyResponse
        {
            public bool status { get; set; }
            public string? message { get; set; }
            public PaystackVerifyData? data { get; set; }
        }

        private class PaystackVerifyData
        {
            public long amount { get; set; }
            public string? status { get; set; }
            public string? reference { get; set; }
            public string? gateway_response { get; set; }
        }

        private class PaystackWebhookEvent
        {
            public string? @event { get; set; }
            public PaystackWebhookData? data { get; set; }
        }

        private class PaystackWebhookData
        {
            public string? reference { get; set; }
            public long amount { get; set; }
            public string? status { get; set; }
        }

        #endregion
    }
}
