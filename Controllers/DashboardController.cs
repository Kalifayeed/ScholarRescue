using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Models.Security;
using ScholarRescue.ViewModels.Order;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Controller for role-specific dashboards.
    /// Client dashboard lives at /Dashboard.
    /// Writer dashboard stays at /Writers/Dashboard.
    /// Admin dashboard stays at /Admin/Dashboard.
    /// </summary>
    public class DashboardController : Controller
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Client dashboard showing orders and stats.
        /// Route: GET /Dashboard
        /// </summary>
        [HttpGet]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public async Task<IActionResult> Index()
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

                return View("~/Views/Orders/Dashboard.cshtml", dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard.");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return RedirectToAction("Index", "Orders");
            }
        }

        /// <summary>
        /// Compatibility redirect for dashboard links that resolve to /Dashboard/Create.
        /// The order form is owned by OrdersController.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = RoleNames.Client + "," + RoleNames.Administrator)]
        public IActionResult Create()
        {
            return RedirectToAction("Create", "Orders", new { area = "" });
        }
    }
}
