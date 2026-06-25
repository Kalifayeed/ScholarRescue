using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Immutable order timeline service. Events can only be created and viewed - never edited or deleted.
    /// </summary>
    public interface IOrderTimelineService
    {
        /// <summary>Adds a new event to the order timeline.</summary>
        Task AddEventAsync(int orderId, string createdByUserId, string createdByName,
            TimelineEventType eventType, string title, string? description = null,
            string? metadataJson = null, bool visibleToClient = true,
            bool visibleToWriter = true, bool visibleToAdmin = true);

        /// <summary>Gets timeline events for an order, filtered by user role.</summary>
        Task<List<OrderTimelineEvent>> GetEventsAsync(int orderId, string? role = null,
            TimelineEventType? typeFilter = null, int page = 1, int pageSize = 100);

        /// <summary>Gets the progress percentage (0-100) based on order status.</summary>
        int GetProgressPercentage(OrderStatus status);

        /// <summary>Gets all events across platform for admin reporting.</summary>
        Task<(List<OrderTimelineEvent> Events, int TotalCount)> GetAllEventsAsync(
            int page = 1, int pageSize = 50, string? search = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            TimelineEventType? typeFilter = null);
    }
}