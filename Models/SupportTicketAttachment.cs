using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    public class SupportTicketAttachment
    {
        public int Id { get; set; }
        [Required] public int TicketId { get; set; }
        [ForeignKey(nameof(TicketId))] public virtual SupportTicket? Ticket { get; set; }
        [Required][MaxLength(255)] public string FileName { get; set; } = string.Empty;
        [Required][MaxLength(500)] public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(450)] public string? UploadedById { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}