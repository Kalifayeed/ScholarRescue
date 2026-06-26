using System.ComponentModel.DataAnnotations;

namespace ScholarRescue.ViewModels.Writer
{
    /// <summary>
    /// ViewModel for placing a monetary bid on an order.
    /// </summary>
    public class PlaceBidViewModel
    {
        public int OrderId { get; set; }

        [Display(Name = "Order")]
        public string OrderTitle { get; set; } = string.Empty;

        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// The amount the writer bids for the order.
        /// </summary>
        [Required(ErrorMessage = "Please enter your bid amount.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bid amount must be greater than 0.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Your Bid Amount ($)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Message/proposal from the writer to the client.
        /// </summary>
        [Required(ErrorMessage = "Please include a brief proposal message.")]
        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters.")]
        [Display(Name = "Proposal Message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Estimated delivery date.
        /// </summary>
        [Display(Name = "Estimated Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime? EstimatedDeliveryDate { get; set; }
    }
}