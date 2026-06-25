namespace ScholarRescue.Services
{
    public class SecureFileService : ISecureFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SecureFileService> _logger;
        private static readonly string[] AllowedMimeTypes = new[]
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "image/jpeg",
            "image/png",
            "image/gif",
            "application/zip",
            "text/plain",
            "text/rtf"
        };

        public SecureFileService(IWebHostEnvironment env, ILogger<SecureFileService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N")[..8];
            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(originalFileName));
            return $"{safeName}_{timestamp}_{guid}{extension}";
        }

        public bool ValidateFileSize(long fileSizeBytes, int maxSizeMb = 25)
        {
            return fileSizeBytes <= maxSizeMb * 1024 * 1024;
        }

        public bool ValidateMimeType(string contentType)
        {
            return AllowedMimeTypes.Contains(contentType.ToLowerInvariant());
        }

        public string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Length > 100 ? sanitized[..100] : sanitized;
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subDirectory)
        {
            var uniqueName = GenerateUniqueFileName(fileName);
            var uploadPath = Path.Combine(_env.WebRootPath, "secure-uploads", subDirectory);
            Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, uniqueName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(stream);

            return Path.Combine("secure-uploads", subDirectory, uniqueName).Replace("\\", "/");
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, filePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found", filePath);

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file: {FilePath}", filePath);
            }
            return Task.CompletedTask;
        }

        public bool IsAuthorizedAccess(string userId, string filePath, string userRole)
        {
            // Admins can access any file
            if (userRole == "Administrator")
                return true;

            // Check if file belongs to user's order, conversation, etc.
            // This is a simplified check - actual implementation would verify ownership
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Writers can only access submission files
            if (userRole == "Writer" && filePath.Contains("/submissions/"))
                return true;

            // Clients can access their order files
            if (filePath.Contains($"/orders/{userId}/"))
                return true;

            return false;
        }

        public string GetSecureFileUrl(string filePath)
        {
            return $"/secure-files?path={Uri.EscapeDataString(filePath)}";
        }
    }
}