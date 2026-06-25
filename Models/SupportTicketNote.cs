using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    public class SupportTicketNote
    {
        public int Id { get; set; }
        [Required] public int TicketId { get; set; }
        [ForeignKey(nameof(TicketId))] public virtual SupportTicket? Ticket { get; set; }
        [Required] public string Content { get; set; } = string.Empty;
        [Required][MaxLength(450)] public string AuthorId { get; set; } = string.Empty;
        [ForeignKey(nameof(AuthorId))] public virtual ApplicationUser? Author { get; set; }
        public bool IsInternal { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}