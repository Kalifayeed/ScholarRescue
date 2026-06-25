namespace ScholarRescue.Models.Configuration
{
    /// <summary>
    /// Strongly typed Paystack configuration from appsettings.json.
    /// </summary>
    public class PaystackSettings
    {
        public string PublicKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}