namespace ScholarRescue.Models.Configuration
{
    /// <summary>
    /// Configuration settings for financial operations.
    /// Values are loaded from appsettings.json section "FinancialSettings".
    /// </summary>
    public class FinancialSettings
    {
        /// <summary>
        /// The platform commission rate applied to order amounts.
        /// Example: 0.10m = 10% commission.
        /// </summary>
        public decimal CommissionRate { get; set; } = 0.10m;
    }
}