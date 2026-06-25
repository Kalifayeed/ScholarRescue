using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service interface for automatic pricing calculations.
    /// All pricing is server-side; clients never enter a budget.
    /// </summary>
    public interface IPricingService
    {
        /// <summary>Base rate per page by academic level.</summary>
        decimal GetBaseRatePerPage(AcademicLevel level);

        /// <summary>Calculates urgency surcharge per page based on hours until deadline.</summary>
        decimal GetUrgencySurchargePerPage(DateTime deadline);

        /// <summary>Total price = Pages × (baseRate + surcharge).</summary>
        decimal CalculatePrice(AcademicLevel level, int pages, DateTime deadline);

        /// <summary>Calculates word count as Pages × 300.</summary>
        int CalculateWordCount(int pages);

        /// <summary>Gets hours remaining until deadline.</summary>
        double GetHoursUntilDeadline(DateTime deadline);

        /// <summary>Breakdown for display.</summary>
        PriceBreakdown GetPriceBreakdown(AcademicLevel level, int pages, DateTime deadline);
    }

    /// <summary>
    /// Price breakdown DTO for display.
    /// </summary>
    public class PriceBreakdown
    {
        public AcademicLevel AcademicLevel { get; set; }
        public string AcademicLevelName { get; set; } = string.Empty;
        public decimal BaseRatePerPage { get; set; }
        public int Pages { get; set; }
        public decimal BaseTotal { get; set; }
        public decimal UrgencySurchargePerPage { get; set; }
        public decimal UrgencyTotal { get; set; }
        public decimal FinalPrice { get; set; }
        public double HoursUntilDeadline { get; set; }
        public bool HasUrgencySurcharge => UrgencySurchargePerPage > 0;
    }
}