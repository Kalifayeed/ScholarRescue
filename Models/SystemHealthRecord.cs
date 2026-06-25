using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.Models
{
    public class SystemHealthRecord
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Component { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Healthy"; // Healthy, Warning, Critical

        [MaxLength(1000)]
        public string? Message { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        public bool IsOperational { get; set; } = true;

        [MaxLength(500)]
        public string? MetricValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}