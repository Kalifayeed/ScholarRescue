namespace ScholarRescue.ViewModels.WriterResources
{
    /// <summary>
    /// Writer Performance Analytics. Contains summary stats, rank progress, and monthly breakdown.
    /// </summary>
    public class WriterAnalyticsViewModel
    {
        public string WriterId { get; set; } = string.Empty;
        public string WriterName { get; set; } = string.Empty;

        // Summary stats
        public int CompletedOrders { get; set; }
        public int CurrentOrders { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public double RevisionPercentage { get; set; }
        public double DisputePercentage { get; set; }
        public double OnTimeDeliveryPercentage { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal CommissionPaid { get; set; }

        // Rank progress
        public string CurrentRank { get; set; } = string.Empty;
        public int CompletedOrdersToNextRank { get; set; }
        public double RatingToNextRank { get; set; }
        public string? NextRankName { get; set; }

        // Monthly statistics
        public List<MonthlyStatEntry> MonthlyStats { get; set; } = new();
    }

    public class MonthlyStatEntry
    {
        public string Month { get; set; } = string.Empty;          // "Jan 2026"
        public int Year { get; set; }
        public int MonthNumber { get; set; }
        public int OrdersCompleted { get; set; }
        public decimal Earnings { get; set; }
        public decimal Commission { get; set; }
    }
}