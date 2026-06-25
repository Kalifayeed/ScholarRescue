using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a formal dispute opened on an order requiring admin arbitration.
    /// When opened, escrow funds are frozen until resolution.
    /// </summary>
    public class OrderDispute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public virtual ApplicationUser Client { get; set; } = null!;

        [Required]
        public string WriterId { get; set; } = string.Empty;
        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        [Required][MaxLength(200)] public string Title { get; set; } = string.Empty;
        [Required][MaxLength(4000)] public string Description { get; set; } = string.Empty;

        [Required][MaxLength(50)] public string DisputeType { get; set; } = string.Empty;

        [Required][MaxLength(50)] public string Status { get; set; } = "Open";

        [MaxLength(2000)] public string? Resolution { get; set; }

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public string? ResolvedByAdminId { get; set; }
        [ForeignKey(nameof(ResolvedByAdminId))]
        public virtual ApplicationUser? ResolvedByAdmin { get; set; }
    }

    public class DisputeEvidence
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DisputeId { get; set; }
        [ForeignKey(nameof(DisputeId))]
        public virtual OrderDispute Dispute { get; set; } = null!;

        [Required]
        public string UploadedBy { get; set; } = string.Empty;

        [Required][MaxLength(255)] public string FileName { get; set; } = string.Empty;
        [Required][MaxLength(500)] public string FilePath { get; set; } = string.Empty;

        [MaxLength(1000)] public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RevisionAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RevisionRequestId { get; set; }

        [Required][MaxLength(255)] public string FileName { get; set; } = string.Empty;
        [Required][MaxLength(500)] public string FilePath { get; set; } = string.Empty;

        [Required] public string UploadedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}