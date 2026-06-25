using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models.Security
{
    public class FraudIncident
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DetectionType { get; set; } = string.Empty; // EmailSharing, PhoneSharing, ExternalPayment, DirectContact

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string Severity { get; set; } = "Warning"; // Warning, TemporarySuspension, PermanentBan

        [Required, MaxLength(100)]
        public string Action { get; set; } = "Warning"; // Warning, TemporarySuspension, PermanentBan

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? ReviewedById { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public bool IsResolved { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }
    }

    public class LoginSecurityLog
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IPAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(200)]
        public string? Browser { get; set; }

        [MaxLength(100)]
        public string? OperatingSystem { get; set; }

        [MaxLength(200)]
        public string? DeviceFingerprint { get; set; }

        public bool IsSuccessful { get; set; }

        public DateTime LoginAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? FailureReason { get; set; }

        public bool IsSuspicious { get; set; }

        [MaxLength(500)]
        public string? AlertMessage { get; set; }
    }

    public class AdministrativeActionLog
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty; // WriterApproval, OrderAssignment, EscrowRelease, etc.

        [Required, MaxLength(450)]
        public string PerformedById { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? TargetUserId { get; set; }

        public int? TargetOrderId { get; set; }

        [MaxLength(2000)]
        public string? OldValue { get; set; }

        [MaxLength(2000)]
        public string? NewValue { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IPAddress { get; set; }
    }

    public class WriterQualityScore
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string WriterId { get; set; } = string.Empty;

        public double OverallScore { get; set; }

        public double ClientRatingScore { get; set; } // 40%

        public double OnTimeDeliveryScore { get; set; } // 25%

        public double RevisionRateScore { get; set; } // 15%

        public double DisputeRateScore { get; set; } // 10%

        public double ReliabilityScore { get; set; } // 10%

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        public int CompletedOrders { get; set; }

        public double OnTimePercentage { get; set; }

        public double RevisionPercentage { get; set; }

        public double DisputePercentage { get; set; }
    }

    public class WriterTier
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string WriterId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Tier { get; set; } = "Beginner"; // Beginner, Intermediate, Senior, Elite

        public decimal MaxOrderValue { get; set; } = 50m;

        public int CompletedOrders { get; set; }

        public double QualityScore { get; set; }

        public bool IsAutoPromoted { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastPromotedAt { get; set; }

        public DateTime? LastDemotedAt { get; set; }

        [MaxLength(500)]
        public string? PromotionReason { get; set; }
    }
}