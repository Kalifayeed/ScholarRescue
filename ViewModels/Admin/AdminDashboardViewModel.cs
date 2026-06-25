namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// Operations Center dashboard with real-time platform metrics.
    /// </summary>
    public class AdminDashboardViewModel
    {
        // ── Users ──
        public int TotalUsers { get; set; }
        public string CurrentUserRole { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public int TotalStaff { get; set; }
        public int TotalWriters { get; set; }
        public int ActiveWriters { get; set; }

        // ── Orders ──
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisMonth { get; set; }
        public int PendingOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int OrdersAwaitingAssignment { get; set; }

        // ── Revenue ──
        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal EscrowBalance { get; set; }

        // ── Operations ──
        public int PendingApplications { get; set; }
        public int OpenDisputes { get; set; }
        public int PendingRevisions { get; set; }
        public int OpenFraudAlerts { get; set; }
        public int PendingQaOrders { get; set; }

        // ── Support Tickets ──
        public int OpenTickets { get; set; }
        public int PendingTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int TotalTickets { get; set; }
    }
}
