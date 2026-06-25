using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public interface IErrorLogService
    {
        Task LogErrorAsync(string category, string errorMessage, string? stackTrace = null, string? userId = null, string? url = null);
        Task<List<ErrorLog>> GetErrorsAsync(string? category = null, bool? resolved = null, int page = 1, int pageSize = 50);
        Task<int> GetErrorCountAsync(string? category = null);
        Task<ErrorLog?> GetErrorByIdAsync(int id);
        Task ResolveErrorAsync(int id, string resolvedById, string? notes = null);
        Task<int> GetUnresolvedCountAsync();
    }
}