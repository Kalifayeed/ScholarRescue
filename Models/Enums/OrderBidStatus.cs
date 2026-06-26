namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Represents the status of a writer's monetary bid on an order.
    /// </summary>
    public enum OrderBidStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Withdrawn = 3
    }
}