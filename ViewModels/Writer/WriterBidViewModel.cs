using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Writer
{
    /// <summary>
    /// ViewModel for displaying a writer's bid on the dashboard.
    /// </summary>
    public class WriterBidViewModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public OrderBidStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}