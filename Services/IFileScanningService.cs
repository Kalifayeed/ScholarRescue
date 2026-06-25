namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for scanning uploaded files for prohibited content (phone numbers, emails, social media, payment requests).
    /// Prevents bypassing chat restrictions via document uploads.
    /// </summary>
    public interface IFileScanningService
    {
        /// <summary>Scans a file for prohibited content. Returns scan result.</summary>
        Task<FileScanResult> ScanFileAsync(string filePath, string fileName, string uploadedById, int? orderId = null);

        /// <summary>Scans raw text content (extracted from a file).</summary>
        Task<FileScanResult> ScanTextContentAsync(string text, string fileName, string uploadedById, int? orderId = null);

        /// <summary>Extracts text from a file for scanning (supports .txt, .csv, marks .docx/.pdf/.zip for review).</summary>
        Task<string?> ExtractTextAsync(string filePath, string fileName);
    }

    /// <summary>
    /// Result of a file scan operation.
    /// </summary>
    public class FileScanResult
    {
        public bool IsFlagged { get; set; }
        public bool IsBlocked { get; set; }
        public int RiskScore { get; set; }
        public string[] Reasons { get; set; } = Array.Empty<string>();
        public string[] DetectedItems { get; set; } = Array.Empty<string>();
        public int? RiskAssessmentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}