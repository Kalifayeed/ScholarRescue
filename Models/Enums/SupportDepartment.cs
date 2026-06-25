namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Internal support departments for ticket routing.
    /// </summary>
    public enum SupportDepartment
    {
        GeneralSupport = 0,
        Orders = 1,
        WriterApplications = 2,
        BillingPayments = 3,
        DisputesCompliance = 4,
        TechnicalSupport = 5,
        Administration = 6
    }

    /// <summary>
    /// Status of a support ticket.
    /// </summary>
    public enum TicketStatus
    {
        Open = 0,
        PendingResponse = 1,
        InProgress = 2,
        WaitingForUser = 3,
        Resolved = 4,
        Closed = 5
    }
}