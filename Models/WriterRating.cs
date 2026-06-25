using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Immutable client rating for a writer after order completion.
    /// One rating per order. Ratings can never be modified after submission.
    /// </summary>
    public class WriterRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [Required]
        public string WriterId { get; set; } = string.Empty;
        [ForeignKey(nameof(WriterId))]
        public virtual ApplicationUser Writer { get; set; } = null!;

        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey(nameof(ClientId))]
        public virtual ApplicationUser Client { get; set; } = null!;

        [Required][Range(1, 5)] public int OverallRating { get; set; }
        [Required][Range(1, 5)] public int QualityRating { get; set; }
        [Required][Range(1, 5)] public int CommunicationRating { get; set; }
        [Required][Range(1, 5)] public int DeadlineRating { get; set; }

        [MaxLength(2000)] public string? ReviewText { get; set; }
        public bool WouldHireAgain { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}