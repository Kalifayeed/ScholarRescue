using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models.Configuration
{
    /// <summary>
    /// Maintenance mode configuration from appsettings.json.
    /// When enabled, non-admin users are redirected to a branded maintenance page.
    /// </summary>
    public class MaintenanceModeSettings
    {
        /// <summary>
        /// Whether maintenance mode is active.
        /// Set to true during upgrades to block client/writer access.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Optional display message shown on the maintenance page.
        /// Defaults to a standard message if not provided.
        /// </summary>
        public string DisplayMessage { get; set; } = "We are currently performing scheduled maintenance. We'll be back shortly.";

        /// <summary>
        /// Estimated return time (e.g., "2:00 PM UTC").
        /// Optional — shown on the maintenance page if provided.
        /// </summary>
        public string? EstimatedReturnTime { get; set; }

        /// <summary>
        /// HTTP status code to return for maintenance responses.
        /// Default: 503 Service Unavailable.
        /// </summary>
        public int StatusCode { get; set; } = 503;

        /// <summary>
        /// Whether to allow authenticated admin users through during maintenance.
        /// Default: true.
        /// </summary>
        public bool AllowAdminAccess { get; set; } = true;

        /// <summary>
        /// Comma-separated list of paths that bypass maintenance mode (e.g., "/health", "/paystack-webhook").
        /// Useful for webhooks and health checks that need to remain accessible.
        /// </summary>
        public string BypassPaths { get; set; } = "/health,/Payments/PaystackWebhook";
    }
}