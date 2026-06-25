using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents a file attachment uploaded by a client during order creation.
    /// Supports PDF, DOC, DOCX, PPT, PPTX, XLS, XLSX, TXT, ZIP, RAR, JPG, JPEG, PNG.
    /// Max 25MB per file, max 10 files per order.
    /// </summary>
    public class OrderAttachment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>The order this attachment belongs to.</summary>
        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        /// <summary>Original file name as uploaded by the user.</summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Unique stored file name on disk (GUID + extension).</summary>
        [Required]
        [MaxLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>Relative path to the stored file.</summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>File size in bytes.</summary>
        [Required]
        public long FileSize { get; set; }

        /// <summary>When the file was uploaded.</summary>
        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>The user who uploaded the file.</summary>
        [Required]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey(nameof(UploadedById))]
        public virtual ApplicationUser UploadedBy { get; set; } = null!;
    }
}