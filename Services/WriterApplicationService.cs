using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.ViewModels.Account;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for managing writer applications: registration, status queries, and the
    /// login-time checks that govern what a writer may access.
    /// </summary>
    public interface IWriterApplicationService
    {
        /// <summary>
        /// Creates a new writer application for a freshly registered user.
        /// Validates writer-specific fields, persists the uploaded files, and stores the application.
        /// </summary>
        Task<WriterApplication> CreateWriterApplicationAsync(
            ApplicationUser user,
            RegisterViewModel viewModel,
            string uploadsRoot);

        /// <summary>
        /// Returns the latest writer application for the user (or null if none).
        /// </summary>
        Task<WriterApplication?> GetLatestApplicationAsync(string userId);

        /// <summary>
        /// Returns true if the writer is allowed to operate on the platform
        /// (i.e. their latest application is Approved and the user is active).
        /// </summary>
        Task<bool> IsWriterActiveAsync(string userId);

        /// <summary>
        /// Returns the writer's current access state to drive dashboard rendering.
        /// </summary>
        Task<WriterAccessState> GetAccessStateAsync(string userId);

        /// <summary>
        /// Persists an uploaded writer document and returns the relative path.
        /// </summary>
        Task<string> SaveWriterFileAsync(IFormFile file, string uploadsRoot, string category);
    }

    /// <summary>
    /// Result type returned to controllers to render the writer dashboard.
    /// </summary>
    public class WriterAccessState
    {
        public bool HasApplication { get; set; }
        public WriterApplicationStatus Status { get; set; } = WriterApplicationStatus.Pending;
        public bool CanAccessOrders { get; set; }
        public bool CanAccessMessages { get; set; }
        public bool CanAccessEarnings { get; set; }
        public bool CanAccessPayouts { get; set; }
        public bool IsSuspended { get; set; }
        public string? AdminFeedback { get; set; }
    }

    /// <summary>
    /// Default implementation of <see cref="IWriterApplicationService"/>.
    /// </summary>
    public class WriterApplicationService : IWriterApplicationService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WriterApplicationService> _logger;

        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
        private const long MaxFileSize = 5L * 1024 * 1024; // 5 MB

        public WriterApplicationService(
            ScholarRescueDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<WriterApplicationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<WriterApplication> CreateWriterApplicationAsync(
            ApplicationUser user,
            RegisterViewModel viewModel,
            string uploadsRoot)
        {
            // Validate writer-only fields (controller will reflect these on the form, but double-check here)
            if (string.IsNullOrWhiteSpace(viewModel.PhoneNumber))
                throw new InvalidOperationException("Phone number is required for writer applications.");
            if (string.IsNullOrWhiteSpace(viewModel.HighestQualification))
                throw new InvalidOperationException("Highest academic qualification is required.");
            if (string.IsNullOrWhiteSpace(viewModel.Specialization))
                throw new InvalidOperationException("Specialization is required.");
            if (string.IsNullOrWhiteSpace(viewModel.Biography))
                throw new InvalidOperationException("Professional biography is required.");

            var biographyWordCount = CountWords(viewModel.Biography);
            if (biographyWordCount < 150)
                throw new InvalidOperationException(
                    $"Professional biography must contain at least 150 words (current: {biographyWordCount}).");
            if (biographyWordCount > 500)
                throw new InvalidOperationException(
                    $"Professional biography must not exceed 500 words (current: {biographyWordCount}).");

            // Files are required for writer applications
            if (viewModel.CvFile == null)
                throw new InvalidOperationException("CV upload is required.");
            if (viewModel.DegreeFile == null)
                throw new InvalidOperationException("Degree certificate upload is required.");
            if (viewModel.WritingSampleFile == null)
                throw new InvalidOperationException("Writing sample upload is required.");

            var cvPath = await SaveWriterFileAsync(viewModel.CvFile, uploadsRoot, "cv");
            var degreePath = await SaveWriterFileAsync(viewModel.DegreeFile, uploadsRoot, "degree");
            var samplePath = await SaveWriterFileAsync(viewModel.WritingSampleFile, uploadsRoot, "sample");

            // Map the phone number onto the Identity user record too (so /me and admin views show it)
            if (!string.IsNullOrWhiteSpace(viewModel.PhoneNumber))
            {
                user.PhoneNumber = viewModel.PhoneNumber;
                await _userManager.UpdateAsync(user);
            }

            var application = new WriterApplication
            {
                UserId = user.Id,
                PhoneNumber = viewModel.PhoneNumber!,
                HighestQualification = viewModel.HighestQualification!,
                Specialization = viewModel.Specialization!,
                Biography = viewModel.Biography!,
                CvFilePath = cvPath,
                DegreeFilePath = degreePath,
                WritingSampleFilePath = samplePath,
                ResumePath = cvPath, // legacy alias
                CertificatePath = degreePath, // legacy alias
                Status = WriterApplicationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            _context.WriterApplications.Add(application);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Writer application #{Id} created for user {UserId} ({Email}).",
                application.Id, user.Id, user.Email);

            return application;
        }

        public async Task<WriterApplication?> GetLatestApplicationAsync(string userId)
        {
            return await _context.WriterApplications
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsWriterActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsDeleted) return false;
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Writer")) return false;

            var latest = await GetLatestApplicationAsync(userId);
            return latest != null && latest.Status == WriterApplicationStatus.Approved;
        }

        public async Task<WriterAccessState> GetAccessStateAsync(string userId)
        {
            var state = new WriterAccessState();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return state;

            var latest = await GetLatestApplicationAsync(userId);
            if (latest == null)
            {
                state.HasApplication = false;
                state.Status = WriterApplicationStatus.Pending;
                return state;
            }

            state.HasApplication = true;
            state.Status = latest.Status;
            state.AdminFeedback = latest.AdminComments;
            state.IsSuspended = latest.Status == WriterApplicationStatus.Suspended || !user.IsActive || user.IsDeleted;

            // Writers are unrestricted when approved and not suspended
            bool approved = latest.Status == WriterApplicationStatus.Approved && user.IsActive && !user.IsDeleted;
            state.CanAccessOrders = approved;
            state.CanAccessMessages = approved;
            state.CanAccessEarnings = approved;
            state.CanAccessPayouts = approved;

            return state;
        }

        public async Task<string> SaveWriterFileAsync(IFormFile file, string uploadsRoot, string category)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Empty file upload.");

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException(
                    $"File '{file.FileName}' exceeds the {MaxFileSize / 1024 / 1024}MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(AllowedExtensions, ext) < 0)
                throw new InvalidOperationException(
                    $"File '{file.FileName}' has an unsupported type. Allowed: {string.Join(", ", AllowedExtensions)}");

            var dir = Path.Combine(uploadsRoot, "writer-applications", category);
            Directory.CreateDirectory(dir);

            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            foreach (var invalid in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(invalid, '_');

            var unique = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{safeName}{ext}";
            var fullPath = Path.Combine(dir, unique);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return a web-friendly relative path
            return $"/uploads/writer-applications/{category}/{unique}";
        }

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
