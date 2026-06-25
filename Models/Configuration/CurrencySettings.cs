using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models.Configuration
{
    /// <summary>
    /// Platform currency configuration from appsettings.json.
    /// Default: USD.
    /// Designed for future support of EUR, GBP, KES.
    /// </summary>
    public class CurrencySettings
    {
        /// <summary>
        /// The platform's primary currency for all pricing, wallets, and transactions.
        /// Defaults to USD when not specified.
        /// </summary>
        public PlatformCurrency DefaultCurrency { get; set; } = PlatformCurrency.USD;

        /// <summary>
        /// ISO 4217 currency code sent to payment gateways.
        /// Maps automatically from DefaultCurrency.
        /// </summary>
        public string CurrencyCode => DefaultCurrency switch
        {
            PlatformCurrency.USD => "USD",
            PlatformCurrency.EUR => "EUR",
            PlatformCurrency.GBP => "GBP",
            PlatformCurrency.KES => "KES",
            _ => "USD"
        };

        /// <summary>
        /// Currency symbol for display purposes (e.g., "$", "€", "£", "KSh").
        /// </summary>
        public string CurrencySymbol => DefaultCurrency switch
        {
            PlatformCurrency.USD => "$",
            PlatformCurrency.EUR => "€",
            PlatformCurrency.GBP => "£",
            PlatformCurrency.KES => "KSh",
            _ => "$"
        };
    }
}