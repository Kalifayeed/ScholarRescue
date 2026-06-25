using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    public class OrderTimelineViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string DeadlineDisplay { get; set; } = string.Empty;
        public string TimeRemaining { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public int Progress { get; set; }
        public string StatusColor { get; set; } = "secondary";
        public string Filter { get; set; } = "all";
        public List<OrderTimelineEvent> Events { get; set; } = new();
    }
}