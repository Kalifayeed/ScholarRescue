using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for the Order Monitoring Engine: detects issues with orders,
    /// generates alerts, and provides data for the escalation dashboard.
    /// </summary>
    public interface IOrderMonitoringService
    {
        /// <summary>
        /// Runs the full monitoring check: scans all orders for alert conditions
        /// and creates new alerts where needed (avoids duplicates).
        /// </summary>
        Task RunMonitoringCheckAsync();

        /// <summary>
        /// Returns all unresolved (non-acknowledged) alerts, newest first.
        /// </summary>
        Task<List<MonitoringAlert>> GetActiveAlertsAsync();

        /// <summary>
        /// Returns all alerts (active and acknowledged), newest first.
        /// </summary>
        Task<List<MonitoringAlert>> GetAllAlertsAsync(MonitoringAlertType? typeFilter = null, bool? acknowledged = null);

        /// <summary>
        /// Acknowledge an alert, marking it as seen by an admin.
        /// </summary>
        Task AcknowledgeAlertAsync(int alertId, string adminId);

        /// <summary>
        /// Resolve an alert (condition has been fixed).
        /// </summary>
        Task ResolveAlertAsync(int alertId);

        /// <summary>
        /// Gets the count of active (unacknowledged) alerts.
        /// </summary>
        Task<int> GetActiveAlertCountAsync();
    }
}