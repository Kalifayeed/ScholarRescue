namespace ScholarRescue.Services
{
    public interface ISecureFileService
    {
        string GenerateUniqueFileName(string originalFileName);
        bool ValidateFileSize(long fileSizeBytes, int maxSizeMb = 25);
        bool ValidateMimeType(string contentType);
        string SanitizeFileName(string fileName);
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string subDirectory);
        Task<byte[]> ReadFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
        bool IsAuthorizedAccess(string userId, string filePath, string userRole);
        string GetSecureFileUrl(string filePath);
    }
}