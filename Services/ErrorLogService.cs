using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly ScholarRescueDbContext _context;

        public ErrorLogService(ScholarRescueDbContext context)
        {
            _context = context;
        }

        public async Task LogErrorAsync(string category, string errorMessage, string? stackTrace = null, string? userId = null, string? url = null)
        {
            var error = new ErrorLog
            {
                Category = category,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                UserId = userId,
                Url = url,
                Timestamp = DateTime.UtcNow
            };

            _context.Set<ErrorLog>().Add(error);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ErrorLog>> GetErrorsAsync(string? category = null, bool? resolved = null, int page = 1, int pageSize = 50)
        {
            var query = _context.Set<ErrorLog>().AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(e => e.Category == category);

            if (resolved.HasValue)
                query = query.Where(e => e.IsResolved == resolved.Value);

            return await query
                .OrderByDescending(e => e.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetErrorCountAsync(string? category = null)
        {
            if (string.IsNullOrEmpty(category))
                return await _context.Set<ErrorLog>().CountAsync();
            return await _context.Set<ErrorLog>().Where(e => e.Category == category).CountAsync();
        }

        public async Task<ErrorLog?> GetErrorByIdAsync(int id)
        {
            return await _context.Set<ErrorLog>().FindAsync(id);
        }

        public async Task ResolveErrorAsync(int id, string resolvedById, string? notes = null)
        {
            var error = await _context.Set<ErrorLog>().FindAsync(id);
            if (error != null)
            {
                error.IsResolved = true;
                error.ResolvedById = resolvedById;
                error.ResolvedAt = DateTime.UtcNow;
                error.ResolutionNotes = notes;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnresolvedCountAsync()
        {
            return await _context.Set<ErrorLog>().CountAsync(e => !e.IsResolved);
        }
    }
}