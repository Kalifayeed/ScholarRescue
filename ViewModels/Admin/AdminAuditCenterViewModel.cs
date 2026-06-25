using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// Single-screen admin command center showing all critical system alerts in one place.
    /// Aggregates failed payments, pending payouts, disputes, fraud, flagged content,
    /// writer penalties, support tickets, and system errors.
    /// </summary>
    public class AdminAuditCenterViewModel
    {
        // ── Count Summary ──────────────────────────────────
        public int FailedPaymentsCount { get; set; }
        public int PendingPayoutsCount { get; set; }
        public int ActiveDisputesCount { get; set; }
        public int FraudAlertsCount { get; set; }
        public int FlaggedMessagesCount { get; set; }
        public int WriterPenaltiesCount { get; set; }
        public int ClientComplaintsCount { get; set; }
        public int SystemErrorsCount { get; set; }

        // ── Detailed Lists ─────────────────────────────────
        public List<PaymentSummary> FailedPayments { get; set; } = new();
        public List<PayoutSummary> PendingPayouts { get; set; } = new();
        public List<DisputeSummary> ActiveDisputes { get; set; } = new();
        public List<FraudAlertSummary> FraudAlerts { get; set; } = new();
        public List<FlaggedMessageSummary> FlaggedMessages { get; set; } = new();
        public List<WriterPenaltySummary> WriterPenalties { get; set; } = new();
        public List<SupportTicketSummary> ClientComplaints { get; set; } = new();
        public List<ErrorLogSummary> SystemErrors { get; set; } = new();
    }

    public class PaymentSummary
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
    }

    public class PayoutSummary
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? WriterName { get; set; }
        public string? WriterEmail { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class DisputeSummary
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string RaisedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class FraudAlertSummary
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string FraudType { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
    }

    public class FlaggedMessageSummary
    {
        public int Id { get; set; }
        public string? SenderName { get; set; }
        public string? SenderEmail { get; set; }
        public string? ContentPreview { get; set; }
        public DateTime SentAt { get; set; }
        public string? ConversationTitle { get; set; }
    }

    public class WriterPenaltySummary
    {
        public int Id { get; set; }
        public string? WriterName { get; set; }
        public string? WriterEmail { get; set; }
        public decimal PenaltyAmount { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPaid { get; set; }
    }

    public class SupportTicketSummary
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ErrorLogSummary
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? UserName { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
    }
}