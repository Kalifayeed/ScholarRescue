namespace ScholarRescue.Models
{
    /// <summary>
    /// Shared validation constants for order attachments (client-uploaded files).
    /// These apply to the initial upload during order creation — not writer submissions.
    /// </summary>
    public static class OrderAttachmentValidation
    {
        /// <summary>
        /// Maximum number of files allowed per order.
        /// </summary>
        public const int MaxFilesPerOrder = 10;

        /// <summary>
        /// Maximum size per file in bytes (25 MB).
        /// </summary>
        public const long MaxFileSizeBytes = 25L * 1024 * 1024;

        /// <summary>
        /// Allowed file extensions for client-uploaded attachments.
        /// </summary>
        public static readonly string[] AllowedExtensions = new[]
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx",
            ".xls", ".xlsx", ".txt", ".zip", ".rar",
            ".jpg", ".jpeg", ".png"
        };
    }
}