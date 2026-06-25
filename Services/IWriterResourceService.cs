using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Business logic for the Writer Knowledge Center content management.
    /// </summary>
    public interface IWriterResourceService
    {
        /// <summary>
        /// Get all active resources for a given category, ordered by SortOrder.
        /// </summary>
        Task<List<WriterResource>> GetByCategoryAsync(WriterResourceCategory category);

        /// <summary>
        /// Get a single resource by ID.
        /// </summary>
        Task<WriterResource?> GetByIdAsync(int id);

        /// <summary>
        /// Search FAQ entries by keyword across Question, Title, Content, and Tags.
        /// </summary>
        Task<List<WriterResource>> SearchFaqAsync(string query);

        /// <summary>
        /// Get all active FAQ entries grouped by SubCategory.
        /// </summary>
        Task<List<WriterResource>> GetFaqAsync();

        /// <summary>
        /// Get distinct sub-categories for a given category.
        /// </summary>
        Task<List<string>> GetSubCategoriesAsync(WriterResourceCategory category);

        /// <summary>
        /// Admin: get all resources (including inactive) for management.
        /// </summary>
        Task<List<WriterResource>> GetAllForAdminAsync(WriterResourceCategory? filterCategory);

        /// <summary>
        /// Admin: create a new resource.
        /// </summary>
        Task<WriterResource> CreateAsync(WriterResource resource, string authorId);

        /// <summary>
        /// Admin: update an existing resource.
        /// </summary>
        Task<WriterResource?> UpdateAsync(WriterResource resource, string authorId);

        /// <summary>
        /// Admin: delete a resource.
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Admin: toggle the IsActive status of a resource.
        /// </summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}