namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for the Client Dashboard, providing a summary overview
    /// of the client's orders and activity.
    /// </summary>
    public class ClientDashboardViewModel
    {
        /// <summary>
        /// Total number of orders placed by the client.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Number of orders currently open (Open or PendingReview status).
        /// </summary>
        public int OpenOrders { get; set; }

        /// <summary>
        /// Number of orders currently in progress (Assigned or InProgress status).
        /// </summary>
        public int InProgressOrders { get; set; }

        /// <summary>
        /// Number of orders that have been completed (Completed status).
        /// </summary>
        public int CompletedOrders { get; set; }

        /// <summary>
        /// The most recent orders placed by the client, for quick reference.
        /// </summary>
        public List<OrderIndexViewModel> RecentOrders { get; set; } = new();
    }
}