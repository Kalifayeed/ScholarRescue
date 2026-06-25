namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Supported platform currencies.
    /// All order pricing, wallet balances, and transactions use the configured currency.
    /// Default: USD.
    /// Designed for extensibility — EUR, GBP, KES may be activated in future.
    /// </summary>
    public enum PlatformCurrency
    {
        /// <summary>
        /// United States Dollar (default).
        /// </summary>
        USD = 0,

        /// <summary>
        /// Euro — reserved for future support.
        /// </summary>
        EUR = 1,

        /// <summary>
        /// British Pound — reserved for future support.
        /// </summary>
        GBP = 2,

        /// <summary>
        /// Kenyan Shilling — reserved for future support.
        /// </summary>
        KES = 3
    }
}