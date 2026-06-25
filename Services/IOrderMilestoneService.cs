using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Progressive delivery milestone management: create, upload, approve.
    /// </summary>
    public interface IOrderMilestoneService
    {
        /// <summary>
        /// Get all milestones for an order, ordered by SortOrder.
        /// </summary>
        Task<List<OrderMilestone>> GetMilestonesAsync(int orderId);

        /// <summary>
        /// Get a single milestone by ID.
        /// </summary>
        Task<OrderMilestone?> GetByIdAsync(int id);

        /// <summary>
        /// Get the files attached to a milestone.
        /// </summary>
        Task<List<OrderMilestoneFile>> GetFilesAsync(int milestoneId);

        /// <summary>
        /// Determines if progressive delivery is required for an order based on page count.
        /// Required: 40+ pages. Optional: 20-39 pages. Not used: <20 pages.
        /// </summary>
        bool IsProgressiveDeliveryRequired(int pageCount);

        bool IsProgressiveDeliveryOptional(int pageCount);

        /// <summary>
        /// Admin creates a new milestone on an order.
        /// </summary>
        Task<OrderMilestone> CreateMilestoneAsync(OrderMilestone milestone);

        /// <summary>
        /// Admin updates an existing milestone.
        /// </summary>
        Task<OrderMilestone?> UpdateMilestoneAsync(OrderMilestone milestone);

        /// <summary>
        /// Admin deletes a milestone (only when not yet approved).
        /// </summary>
        Task<bool> DeleteMilestoneAsync(int id);

        /// <summary>
        /// Writer uploads milestone files and marks as Submitted.
        /// </summary>
        Task<OrderMilestone> SubmitMilestoneAsync(int milestoneId, string writerId, List<(string FileName, string FilePath, long FileSize, string? Description)> files, string? notes);

        /// <summary>
        /// Client approves a Submitted milestone. Records earnings in the ledger.
        /// </summary>
        Task<OrderMilestone> ApproveMilestoneAsync(int milestoneId, string clientId, string? notes);

        /// <summary>
        /// Get the next-sort-order index for a new milestone on this order.
        /// </summary>
        Task<int> GetNextSortOrderAsync(int orderId);

        /// <summary>
        /// Get a human-readable timeline of all milestones and their files.
        /// </summary>
        Task<List<MilestoneTimelineEntry>> GetTimelineAsync(int orderId);
    }

    /// <summary>
    /// Display-friendly timeline entry.
    /// </summary>
    public class MilestoneTimelineEntry
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Pages { get; set; }
        public DateTime Deadline { get; set; }
        public MilestoneStatus Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public decimal ApprovedEarnings { get; set; }
        public List<MilestoneFileEntry> Files { get; set; } = new();
    }

    public class MilestoneFileEntry
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
