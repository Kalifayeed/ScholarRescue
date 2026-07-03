using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarRescue.Models;
using ScholarRescue.Services;
using ScholarRescue.Models.Security;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller for financial management (admin only).
    /// Provides financial dashboard, payout management, reports, and transaction history.
    /// </summary>
    [Authorize(Roles = RoleNames.Administrator)]
    public class FinanceController : Controller
    {
        private readonly IFinancialService _financialService;
        private readonly IPayoutWindowService _payoutWindowService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FinanceController> _logger;

        public FinanceController(
            IFinancialService financialService,
            IPayoutWindowService payoutWindowService,
            UserManager<ApplicationUser> userManager,
            ILogger<FinanceController> logger)
        {
            _financialService = financialService;
            _payoutWindowService = payoutWindowService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Admin financial dashboard with platform revenue, commission, and payout overview.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var dashboard = await _financialService.GetAdminDashboardAsync();
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading financial dashboard.");
                TempData["ErrorMessage"] = "Error loading financial dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Displays all payout requests for admin management.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Payouts(string? status = null)
        {
            try
            {
                List<PayoutRequest> payouts;

                if (status == "pending")
                    payouts = await _financialService.GetPendingPayoutsAsync();
                else if (status == "approved")
                    payouts = (await _financialService.GetAllPayoutsAsync())
                        .Where(p => p.Status == PayoutStatus.Approved).ToList();
                else if (status == "paid")
                    payouts = (await _financialService.GetAllPayoutsAsync())
                        .Where(p => p.Status == PayoutStatus.Paid).ToList();
                else if (status == "rejected")
                    payouts = (await _financialService.GetAllPayoutsAsync())
                        .Where(p => p.Status == PayoutStatus.Rejected).ToList();
                else
                    payouts = await _financialService.GetAllPayoutsAsync();

                ViewBag.CurrentFilter = status;
                ViewBag.PayoutWindowOpen = _payoutWindowService.IsPayoutWindowOpen();
                ViewBag.PayoutWindowMessage = _payoutWindowService.GetPayoutWindowMessage();
                return View(payouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payouts.");
                TempData["ErrorMessage"] = "Error loading payout requests.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Approves a pending payout request.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePayout(int id)
        {
            try
            {
                var adminUser = await GetCurrentUserIdAsync();
                await _financialService.ApprovePayoutAsync(id, adminUser);
                TempData["SuccessMessage"] = "Payout approved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payout {PayoutId}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Payouts));
        }

        /// <summary>
        /// Rejects a pending payout request.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayout(int id, string? notes)
        {
            try
            {
                var adminUser = await GetCurrentUserIdAsync();
                await _financialService.RejectPayoutAsync(id, adminUser, notes);
                TempData["SuccessMessage"] = "Payout rejected. Funds returned to writer's wallet.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payout {PayoutId}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Payouts));
        }

        /// <summary>
        /// Marks an approved payout as paid.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPayoutPaid(int id)
        {
            try
            {
                var adminUser = await GetCurrentUserIdAsync();
                await _financialService.MarkPayoutPaidAsync(id, adminUser);
                TempData["SuccessMessage"] = "Payout marked as paid.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking payout {PayoutId} as paid.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Payouts));
        }

        /// <summary>
        /// Revenue report page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RevenueReport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var report = await _financialService.GetRevenueReportAsync(fromDate, toDate);
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading revenue report.");
                TempData["ErrorMessage"] = "Error loading report.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Commission report page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CommissionReport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var report = await _financialService.GetCommissionReportAsync(fromDate, toDate);
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading commission report.");
                TempData["ErrorMessage"] = "Error loading report.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Writer earnings report page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> WriterEarningsReport(DateTime? fromDate, DateTime? toDate, string? writerId)
        {
            try
            {
                var report = await _financialService.GetWriterEarningsReportAsync(fromDate, toDate, writerId);
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.WriterId = writerId;
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading writer earnings report.");
                TempData["ErrorMessage"] = "Error loading report.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Payout report page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PayoutReport(DateTime? fromDate, DateTime? toDate, string? writerId)
        {
            try
            {
                var report = await _financialService.GetPayoutReportAsync(fromDate, toDate, writerId);
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.WriterId = writerId;
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payout report.");
                TempData["ErrorMessage"] = "Error loading report.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Transaction history page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Transactions(int page = 1)
        {
            try
            {
                var transactions = await _financialService.GetAllTransactionsAsync(page, 50);
                ViewBag.CurrentPage = page;
                return View(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transactions.");
                TempData["ErrorMessage"] = "Error loading transactions.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        /// <summary>
        /// Gets the current authenticated user's ID.
        /// </summary>
        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                throw new InvalidOperationException("User not found.");
            return user.Id;
        }
    }
}
