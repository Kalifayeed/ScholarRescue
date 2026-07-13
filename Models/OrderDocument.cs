using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a file/document attached to an order, such as instructions,
    /// reference materials, or completed work.
    /// </summary>
    public class OrderDocument
    {
        /// <summary>
        /// Primary key for the document.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key referencing the order this document belongs to.
        /// </summary>
        [Required]
        public int OrderId { get; set; }

        /// <summary>
        /// Navigation property for the parent tutoring request.
        /// </summary>
        [ForeignKey(nameof(OrderId))]
        public virtual TutoringRequest Order { get; set; } = null!;

        /// <summary>
        /// The original file name as uploaded by the user.
        /// </summary>
        [Required]
        [MaxLength(500)]
        [Display(Name = "File Name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The stored file path on disk.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        [Display(Name = "File Size (bytes)")]
        public long FileSize { get; set; }

        /// <summary>
        /// The MIME content type of the uploaded file.
        /// </summary>
        [MaxLength(200)]
        [Display(Name = "Content Type")]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Category of the document: Instructions, Reference, Draft, Completed.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key referencing the user who uploaded the document.
        /// </summary>
        [Required]
        public string UploadedById { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for the user who uploaded the document.
        /// </summary>
        [ForeignKey(nameof(UploadedById))]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;

        /// <summary>
        /// Timestamp when the document was uploaded.
        /// </summary>
        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}