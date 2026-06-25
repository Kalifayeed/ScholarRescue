using ScholarRescue.Models;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for the audit logs list page with search and pagination.
    /// </summary>
    public class AuditLogsViewModel
    {
        public List<AuditLog> Logs { get; set; } = new();
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}