using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Implements CRUD and search operations for Writer Knowledge Center resources.
    /// </summary>
    public class WriterResourceService : IWriterResourceService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<WriterResourceService> _logger;

        public WriterResourceService(
            ScholarRescueDbContext context,
            ILogger<WriterResourceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<WriterResource>> GetByCategoryAsync(WriterResourceCategory category)
        {
            return await _context.WriterResources
                .Where(r => r.Category == category && r.IsActive)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Title)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WriterResource?> GetByIdAsync(int id)
        {
            return await _context.WriterResources
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<WriterResource>> SearchFaqAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetFaqAsync();

            var lowerQuery = query.ToLower();

            return await _context.WriterResources
                .Where(r => r.Category == WriterResourceCategory.FAQ && r.IsActive &&
                    (r.Question!.ToLower().Contains(lowerQuery) ||
                     r.Title.ToLower().Contains(lowerQuery) ||
                     r.Content.ToLower().Contains(lowerQuery) ||
                     (r.Tags != null && r.Tags.ToLower().Contains(lowerQuery))))
                .OrderBy(r => r.SubCategory)
                .ThenBy(r => r.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<WriterResource>> GetFaqAsync()
        {
            return await _context.WriterResources
                .Where(r => r.Category == WriterResourceCategory.FAQ && r.IsActive)
                .OrderBy(r => r.SubCategory)
                .ThenBy(r => r.SortOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<string>> GetSubCategoriesAsync(WriterResourceCategory category)
        {
            return await _context.WriterResources
                .Where(r => r.Category == category && r.IsActive && r.SubCategory != null)
                .Select(r => r.SubCategory!)
                .Distinct()
                .OrderBy(s => s)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<WriterResource>> GetAllForAdminAsync(WriterResourceCategory? filterCategory)
        {
            var query = _context.WriterResources.AsQueryable();

            if (filterCategory.HasValue)
                query = query.Where(r => r.Category == filterCategory.Value);

            return await query
                .OrderBy(r => r.Category)
                .ThenBy(r => r.SortOrder)
                .ThenBy(r => r.Title)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<WriterResource> CreateAsync(WriterResource resource, string authorId)
        {
            resource.AuthorId = authorId;
            resource.CreatedAt = DateTime.UtcNow;
            resource.UpdatedAt = DateTime.UtcNow;

            _context.WriterResources.Add(resource);
            await _context.SaveChangesAsync();

            _logger.LogInformation("WriterResource created: {Id} - {Title} by {AuthorId}",
                resource.Id, resource.Title, authorId);

            return resource;
        }

        public async Task<WriterResource?> UpdateAsync(WriterResource resource, string authorId)
        {
            var existing = await _context.WriterResources.FindAsync(resource.Id);
            if (existing == null) return null;

            existing.Title = resource.Title;
            existing.Content = resource.Content;
            existing.Category = resource.Category;
            existing.SubCategory = resource.SubCategory;
            existing.Tags = resource.Tags;
            existing.SortOrder = resource.SortOrder;
            existing.IsActive = resource.IsActive;
            existing.Question = resource.Question;
            existing.AuthorId = authorId;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("WriterResource updated: {Id} - {Title} by {AuthorId}",
                existing.Id, existing.Title, authorId);

            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var resource = await _context.WriterResources.FindAsync(id);
            if (resource == null) return false;

            _context.WriterResources.Remove(resource);
            await _context.SaveChangesAsync();

            _logger.LogInformation("WriterResource deleted: {Id} - {Title}", id, resource.Title);
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var resource = await _context.WriterResources.FindAsync(id);
            if (resource == null) return false;

            resource.IsActive = !resource.IsActive;
            resource.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("WriterResource {Id} IsActive toggled to {IsActive}",
                id, resource.IsActive);

            return true;
        }
    }
}