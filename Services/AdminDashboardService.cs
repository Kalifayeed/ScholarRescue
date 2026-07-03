using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models.Security;
using ScholarRescue.ViewModels.Admin;

namespace ScholarRescue.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ScholarRescueDbContext _context;

        public AdminDashboardService(ScholarRescueDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardViewModelAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalUsers = await _context.Users.CountAsync();
            var totalClients = await _context.Users.CountAsync(u => u.UserType == RoleNames.Client);
            var totalWriters = await _context.Users.CountAsync(u => u.UserType == RoleNames.Writer);
            var activeWriters = await _context.Users.CountAsync(u => u.UserType == RoleNames.Writer && u.IsActive && u.IsAcceptingOrders);

            var totalOrders = await _context.Orders.CountAsync();
            var ordersToday = await _context.Orders.CountAsync(o => o.CreatedAt >= todayStart);
            var ordersThisMonth = await _context.Orders.CountAsync(o => o.CreatedAt >= monthStart);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == Models.Enums.OrderStatus.Open || o.Status == Models.Enums.OrderStatus.PendingReview);
            var inProgressOrders = await _context.Orders.CountAsync(o => o.Status == Models.Enums.OrderStatus.InProgress || o.Status == Models.Enums.OrderStatus.Assigned);
            var awaitingAssignment = await _context.Orders.CountAsync(o => o.Status == Models.Enums.OrderStatus.Open && o.AssignedWriterId == null);

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == Models.Enums.OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.Budget) ?? 0;
            var revenueToday = await _context.Orders
                .Where(o => o.Status == Models.Enums.OrderStatus.Completed && (o.CompletedAt ?? o.UpdatedAt) >= todayStart)
                .SumAsync(o => (decimal?)o.Budget) ?? 0;
            var revenueThisMonth = await _context.Orders
                .Where(o => o.Status == Models.Enums.OrderStatus.Completed && (o.CompletedAt ?? o.UpdatedAt) >= monthStart)
                .SumAsync(o => (decimal?)o.Budget) ?? 0;
            var escrowBalance = await _context.Set<Models.EscrowAccount>()
                .Where(e => e.Status == Models.Enums.EscrowStatus.Funded)
                .SumAsync(e => (decimal?)e.FundedAmount) ?? 0;

            var pendingApplications = await _context.WriterApplications.CountAsync(a => a.Status == Models.Enums.WriterApplicationStatus.Pending);
            var openDisputes = await _context.OrderDisputes.CountAsync(d => d.Status == "Open" || d.Status == "InReview");
            var pendingRevisions = await _context.RevisionRequests.CountAsync(r => r.Status == Models.Enums.RevisionRequestStatus.Pending);
            var openFraudAlerts = await _context.Set<Models.AccountFraudAlert>().CountAsync(a => a.Status == "Open");
            var pendingQa = await _context.Orders.CountAsync(o => o.Status == Models.Enums.OrderStatus.PendingQA);

            var openTickets = await _context.SupportTickets.CountAsync(t => t.Status == Models.Enums.TicketStatus.Open);
            var pendingTickets = await _context.SupportTickets.CountAsync(t => t.Status == Models.Enums.TicketStatus.PendingResponse || t.Status == Models.Enums.TicketStatus.WaitingForUser);
            var inProgressTickets = await _context.SupportTickets.CountAsync(t => t.Status == Models.Enums.TicketStatus.InProgress);
            var resolvedTickets = await _context.SupportTickets.CountAsync(t => t.Status == Models.Enums.TicketStatus.Resolved);
            var totalTickets = await _context.SupportTickets.CountAsync();

            return new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalClients = totalClients,
                TotalStaff = 0,
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
        }
    }
}
