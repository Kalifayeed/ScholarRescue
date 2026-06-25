using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Automatic pricing engine. All pricing is computed server-side.
    /// Clients never enter prices manually. Word count = Pages × 300 (fixed).
    /// </summary>
    public class PricingService : IPricingService
    {
        // Base rates per page by academic level
        private static readonly Dictionary<AcademicLevel, decimal> BaseRates = new()
        {
            [AcademicLevel.HighSchool] = 5m,
            [AcademicLevel.College] = 6m,
            [AcademicLevel.Undergraduate] = 7m,
            [AcademicLevel.Masters] = 8m,
            [AcademicLevel.PhD] = 9m
        };

        // Fixed words per page (system-controlled, not client-modifiable)
        private const int WordsPerPage = 300;

        public decimal GetBaseRatePerPage(AcademicLevel level)
        {
            return BaseRates.GetValueOrDefault(level, 5m);
        }

        public decimal GetUrgencySurchargePerPage(DateTime deadline)
        {
            var hours = GetHoursUntilDeadline(deadline);

            // Deadline ≤ 12 hours: +$2/page
            if (hours <= 12)
                return 2m;

            // Deadline ≤ 24 hours: +$1/page
            if (hours <= 24)
                return 1m;

            // Standard pricing (> 48 hours), but also handle 24-48 range
            return 0m;
        }

        public decimal CalculatePrice(AcademicLevel level, int pages, DateTime deadline)
        {
            var baseRate = GetBaseRatePerPage(level);
            var surcharge = GetUrgencySurchargePerPage(deadline);
            return pages * (baseRate + surcharge);
        }

        public int CalculateWordCount(int pages)
        {
            return pages * WordsPerPage;
        }

        public double GetHoursUntilDeadline(DateTime deadline)
        {
            return (deadline - DateTime.UtcNow).TotalHours;
        }

        public PriceBreakdown GetPriceBreakdown(AcademicLevel level, int pages, DateTime deadline)
        {
            var baseRate = GetBaseRatePerPage(level);
            var surcharge = GetUrgencySurchargePerPage(deadline);
            var hours = GetHoursUntilDeadline(deadline);

            return new PriceBreakdown
            {
                AcademicLevel = level,
                AcademicLevelName = level switch
                {
                    AcademicLevel.HighSchool => "High School",
                    AcademicLevel.College => "College",
                    AcademicLevel.Undergraduate => "Undergraduate",
                    AcademicLevel.Masters => "Master's",
                    AcademicLevel.PhD => "PhD",
                    _ => "Unknown"
                },
                BaseRatePerPage = baseRate,
                Pages = pages,
                BaseTotal = pages * baseRate,
                UrgencySurchargePerPage = surcharge,
                UrgencyTotal = pages * surcharge,
                FinalPrice = pages * (baseRate + surcharge),
                HoursUntilDeadline = hours
            };
        }
    }
}