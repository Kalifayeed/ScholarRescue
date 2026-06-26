using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Admin
{
    public class OrderBidAdminViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
        public string? AssignedWriterName { get; set; }

        public List<AdminBidItemViewModel> Bids { get; set; } = new();
    }

    public class AdminBidItemViewModel
    {
        public int BidId { get; set; }
        public string WriterId { get; set; } = string.Empty;
        public string WriterDisplayName { get; set; } = string.Empty;
        public string WriterEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Message { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public OrderBidStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int WriterApplicationCount { get; set; }
    }
}