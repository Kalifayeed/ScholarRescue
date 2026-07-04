using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for the order list (Index) page.
    /// Provides a summary of each order suitable for table display.
    /// </summary>
    public class OrderIndexViewModel
    {
        /// <summary>
        /// The unique identifier of the order.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique human-readable order number.
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// The title of the academic paper/assignment.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// The client's full name.
        /// </summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// The assigned writer's full name, if assigned.
        /// </summary>
        public string? WriterName { get; set; }

        /// <summary>
        /// Current status of the order.
        /// </summary>
        public OrderStatus Status { get; set; }

        /// <summary>
        /// The deadline for completion.
        /// </summary>
        public DateTime Deadline { get; set; }

        /// <summary>
        /// Number of pages in the client's existing draft (informational).
        /// </summary>
        public int? Pages { get; set; }

        /// <summary>
        /// The total budget for the order.
        /// </summary>
        public decimal Budget { get; set; }

        /// <summary>
        /// Timestamp when the order was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Academic subject of the order.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Academic level required.
        /// </summary>
        public AcademicLevel AcademicLevel { get; set; }

        /// <summary>
        /// Citation format required.
        /// </summary>
        public CitationFormat CitationFormat { get; set; }
    }
}
