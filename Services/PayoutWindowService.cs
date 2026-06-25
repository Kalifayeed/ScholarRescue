namespace ScholarRescue.Services
{
    /// <summary>
    /// Implementation of payout window enforcement.
    /// Windows are open on the 1st and 15th of each month for 24 hours (UTC).
    /// </summary>
    public class PayoutWindowService : IPayoutWindowService
    {
        public bool IsPayoutWindowOpen()
        {
            var now = DateTime.UtcNow;
            return now.Day == 1 || now.Day == 15;
        }

        public DateTime GetNextPayoutDate()
        {
            var now = DateTime.UtcNow;
            return GetNextPayoutWindow(now);
        }

        public string GetPayoutWindowMessage()
        {
            if (IsPayoutWindowOpen())
            {
                var timeUntilClose = GetTimeUntilWindowCloses();
                return $"Payout requests are currently open. Window closes in {timeUntilClose.Hours}h {timeUntilClose.Minutes}m.";
            }

            var nextDate = GetNextPayoutDate();
            var timeUntilOpen = GetTimeUntilNextWindow();
            return $"Payout requests are currently closed. Next window opens {nextDate:MMMM dd, yyyy} at 00:00 UTC (in {timeUntilOpen.Days}d {timeUntilOpen.Hours}h).";
        }

        public TimeSpan GetTimeUntilNextWindow()
        {
            var now = DateTime.UtcNow;
            if (IsPayoutWindowOpen())
                return TimeSpan.Zero;

            var next = GetNextPayoutDate();
            return next - now;
        }

        private TimeSpan GetTimeUntilWindowCloses()
        {
            var now = DateTime.UtcNow;
            var endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, DateTimeKind.Utc);
            return endOfDay - now;
        }

        private static DateTime GetNextPayoutWindow(DateTime now)
        {
            var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var fifteenthOfMonth = new DateTime(now.Year, now.Month, 15, 0, 0, 0, DateTimeKind.Utc);

            if (now.Day < 1) return firstOfMonth;
            if (now.Day < 15) return fifteenthOfMonth;

            // Next month 1st
            var nextMonth = now.AddMonths(1);
            return new DateTime(nextMonth.Year, nextMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        }
    }
}