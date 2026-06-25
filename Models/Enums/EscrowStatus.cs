namespace ScholarRescue.Models.Enums
{
    public enum EscrowStatus
    {
        PendingFunding = 0,
        Funded = 1,
        PartiallyReleased = 2,
        Released = 3,
        Refunded = 4,
        Disputed = 5,
        Cancelled = 6
    }
}