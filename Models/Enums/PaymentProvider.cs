namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Supported payment providers for order checkout.
    /// Designed for future extensibility — add new providers without redesigning the checkout UI.
    /// </summary>
    public enum PaymentProvider
    {
        /// <summary>
        /// Paystack (default) — processes Visa, Mastercard, American Express, and local payment methods.
        /// </summary>
        Paystack = 0,

        /// <summary>
        /// PayPal — reserved for future integration.
        /// </summary>
        PayPal = 1
    }
}