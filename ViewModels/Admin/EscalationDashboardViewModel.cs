using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// View model for the admin escalation dashboard showing monitoring alerts
    /// and summary metrics.
    /// </summary>
    public class EscalationDashboardViewModel
    {
        public List<MonitoringAlert> ActiveAlerts { get; set; } = new();
        public List<MonitoringAlert> RecentResolvedAlerts { get; set; } = new();
        public int TotalActiveAlerts { get; set; }
        public int NoApplicantAlerts { get; set; }
        public int UrgentNoApplicantAlerts { get; set; }
        public int WriterInactiveAlerts { get; set; }
        public int MilestoneOverdueAlerts { get; set; }
        public int RevisionOverdueAlerts { get; set; }
        public MonitoringAlertType? CurrentTypeFilter { get; set; }
    }
}