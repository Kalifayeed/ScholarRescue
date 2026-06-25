namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Every financial event in the system creates an immutable ledger entry
    /// with a specific transaction type.
    /// </summary>
    public enum TransactionType
    {
        OrderCreated = 0,
        OrderFunded = 1,
        OrderCompleted = 2,
        CommissionCharged = 3,
        WriterEarningAdded = 4,
        WriterEarningReleased = 5,
        PayoutRequested = 6,
        PayoutApproved = 7,
        PayoutRejected = 8,
        PayoutPaid = 9,
        RefundIssued = 10,
        Adjustment = 11
    }
}