namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Represents the lifecycle status of an order in the Scholar Rescue platform.
    /// </summary>
    public enum OrderStatus
    {
        Draft = 0,
        PendingReview = 1,
        Open = 2,
        Assigned = 3,
        InProgress = 4,
        DraftSubmitted = 5,
        RevisionRequested = 6,
        RevisionSubmitted = 7,
        FinalSubmitted = 8,
        Completed = 9,
        Cancelled = 10,
        PendingQA = 11,
        Delivered = 12,
        /// <summary>Order created but awaiting payment before becoming visible in marketplace.</summary>
        PendingPayment = 13,

        /// <summary>Payment received and escrow funded.</summary>
        Funded = 14
    }
}