using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Content Moderation & File Scanning Engine.
    /// Scans uploaded documents for prohibited content to prevent chat restriction bypass.
    /// Supports .txt and .csv via direct text extraction; flags .docx, .pdf, .zip for admin review.
    /// </summary>
public class FileScanningService : IFileScanningService
    {
        private readonly IRiskDetectionService _riskDetection;
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FileScanningService> _logger;

        // File extensions that can be fully scanned (text-extractable)
        private static readonly HashSet<string> ScanableExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".csv", ".log", ".md", ".rtf", ".xml", ".json", ".html", ".htm", ".css", ".js"
        };

        // File extensions that require admin review (binary formats)
        private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".docx", ".doc", ".pdf", ".zip", ".rar", ".7z", ".xlsx", ".pptx"
        };

        public FileScanningService(
            IRiskDetectionService riskDetection,
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<FileScanningService> logger)
        {
            _riskDetection = riskDetection;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<FileScanResult> ScanFileAsync(string filePath, string fileName, string uploadedById, int? orderId = null)
        {
            // Check file extension
            var ext = Path.GetExtension(fileName);

            if (BinaryExtensions.Contains(ext))
            {
                // Binary files - flag for admin review, don't block automatically
                var result = new FileScanResult
                {
                    IsFlagged = true,
                    IsBlocked = false,
                    RiskScore = 10,
                    Reasons = new[] { $"Binary file type ({ext}) - requires manual admin review" },
                    DetectedItems = Array.Empty<string>(),
                    Message = $"File '{fileName}' ({ext}) has been flagged for admin review."
                };

                // Create risk assessment for admin awareness
                var assessment = await _riskDetection.CreateRiskAssessmentAsync(
                    "File", fileName, RiskCategory.OperationalRisk, 10,
                    $"Binary file uploaded: {fileName} ({ext})", null, orderId, null, false);

                result.RiskAssessmentId = assessment.Id;

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "Binary File Flagged",
                    PerformedById = uploadedById,
                    Description = $"Binary file '{fileName}' ({ext}) flagged for admin review",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("Binary file flagged for review: {FileName} ({Ext}) by {User}", fileName, ext, uploadedById);
                return result;
            }

            // Try to extract text from the file
            var text = await ExtractTextAsync(filePath, fileName);

            if (string.IsNullOrWhiteSpace(text))
            {
                // Cannot scan - flag but allow
                return new FileScanResult
                {
                    IsFlagged = true,
                    IsBlocked = false,
                    RiskScore = 5,
                    Reasons = new[] { "Unable to extract text for scanning" },
                    DetectedItems = Array.Empty<string>(),
                    Message = "File could not be scanned. Admin review recommended."
                };
            }

            // Scan the extracted text using the risk detection service
            return await ScanTextContentAsync(text, fileName, uploadedById, orderId);
        }

        public async Task<FileScanResult> ScanTextContentAsync(string text, string fileName, string uploadedById, int? orderId = null)
        {
            var reasons = new List<string>();
            var detectedItems = new List<string>();
            int totalScore = 0;

            // Use RiskDetectionService patterns to scan text
            if (_riskDetection.ContainsPhoneNumber(text, out var phone))
            {
                totalScore += 25;
                reasons.Add("Phone number detected in file");
                detectedItems.Add(phone ?? "phone");
            }

            if (_riskDetection.ContainsEmailAddress(text, out var email))
            {
                totalScore += 25;
                reasons.Add("Email address detected in file");
                detectedItems.Add(email ?? "email");
            }

            if (_riskDetection.ContainsSocialMedia(text, out var social))
            {
                totalScore += 20;
                reasons.Add("Social media contact detected in file");
                detectedItems.Add(social ?? "social");
            }

            if (_riskDetection.ContainsExternalPaymentRequest(text, out var payment))
            {
                totalScore += 40;
                reasons.Add("External payment request detected in file");
                detectedItems.Add(payment ?? "payment");
            }

            bool isFlagged = totalScore > 0;
            bool isBlocked = totalScore >= 25; // Block files with phone/email at minimum

            var result = new FileScanResult
            {
                IsFlagged = isFlagged,
                IsBlocked = isBlocked,
                RiskScore = totalScore,
                Reasons = reasons.ToArray(),
                DetectedItems = detectedItems.ToArray(),
                Message = isBlocked
                    ? $"File '{fileName}' has been blocked: {string.Join(", ", reasons)}"
                    : isFlagged
                        ? $"File '{fileName}' flagged: {string.Join(", ", reasons)}"
                        : "File scan passed - no prohibited content detected."
            };

            if (isFlagged)
            {
                var category = reasons.Any(r => r.Contains("payment", StringComparison.OrdinalIgnoreCase))
                    ? RiskCategory.CommissionAvoidance
                    : RiskCategory.CommunicationRisk;

                var assessment = await _riskDetection.CreateRiskAssessmentAsync(
                    "File", fileName, category, totalScore,
                    string.Join("; ", reasons), null, orderId,
                    string.Join(", ", detectedItems), isBlocked);

                result.RiskAssessmentId = assessment.Id;

                // Update user risk profile
                var user = await _userManager.FindByIdAsync(uploadedById);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "Client";
                    await _riskDetection.UpdateRiskProfileAsync(uploadedById, totalScore, role);
                }

                // Audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = isBlocked ? "File Blocked" : "File Flagged",
                    PerformedById = uploadedById,
                    Description = $"File '{fileName}' {(isBlocked ? "blocked" : "flagged")}: {string.Join("; ", reasons)}",
                    CreatedDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogWarning("File {FileName} by {User}: {Reasons} (Score: {Score}, Blocked: {Blocked})",
                    fileName, uploadedById, string.Join(", ", reasons), totalScore, isBlocked);
            }

            return result;
        }

        public async Task<string?> ExtractTextAsync(string filePath, string fileName)
        {
            var ext = Path.GetExtension(fileName);

            if (!ScanableExtensions.Contains(ext))
            {
                // Binary format - cannot extract text without external library
                _logger.LogDebug("Cannot extract text from {FileName}: unsupported extension {Ext}", fileName, ext);
                return null;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found for scanning: {FilePath}", filePath);
                    return null;
                }

                var text = await File.ReadAllTextAsync(filePath);

                // Limit scan to first 100KB to prevent performance issues
                if (text.Length > 100_000)
                {
                    text = text[..100_000];
                }

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from {FileName}", fileName);
                return null;
            }
        }
    }
}