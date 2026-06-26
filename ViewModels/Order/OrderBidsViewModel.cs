using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Order
{
    /// <summary>
    /// ViewModel for displaying bids submitted on a client's order.
    /// Privacy: Clients see anonymous writer labels; Admin sees real identities.
    /// </summary>
    public class OrderBidsViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderTitle { get; set; } = string.Empty;
        public string? OrderSubject { get; set; }
        public bool IsClientOwner { get; set; }
        public bool IsAdmin { get; set; }

        public List<BidItemViewModel> Bids { get; set; } = new();
    }

    public class BidItemViewModel
    {
        public int BidId { get; set; }
        public string WriterId { get; set; } = string.Empty;

        /// <summary>
        /// Display name shown to the viewer. Admins see real writer name; clients see anonymous label.
        /// </summary>
        public string WriterDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Writer email. Only populated for admin viewers.
        /// </summary>
        public string? WriterEmail { get; set; }
        public decimal Amount { get; set; }
        public string? Message { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public OrderBidStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
