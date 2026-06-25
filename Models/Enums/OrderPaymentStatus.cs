namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Payment status for orders in the Stripe payment workflow.
    /// </summary>
    public enum OrderPaymentStatus
    {
        PendingPayment = 0,
        Paid = 1,
        EscrowFunded = 2,
        Released = 3,
        Refunded = 4
    }
}