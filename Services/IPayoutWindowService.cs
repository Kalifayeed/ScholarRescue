namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for payout window enforcement.
    /// Payout requests are only allowed on the 1st and 15th of each month.
    /// </summary>
    public interface IPayoutWindowService
    {
        /// <summary>Checks if the payout window is currently open.</summary>
        bool IsPayoutWindowOpen();

        /// <summary>Gets the next payout date.</summary>
        DateTime GetNextPayoutDate();

        /// <summary>Gets a human-readable message about payout window status.</summary>
        string GetPayoutWindowMessage();

        /// <summary>Gets the remaining time until the window closes (if open) or opens (if closed).</summary>
        TimeSpan GetTimeUntilNextWindow();
    }
}