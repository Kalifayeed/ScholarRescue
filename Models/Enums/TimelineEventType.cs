namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Defines all event types that can appear in an order timeline.
    /// </summary>
    public enum TimelineEventType
    {
        OrderCreated = 0,
        PaymentReceived = 1,
        OrderPublished = 2,
        WriterApplied = 3,
        WriterAssigned = 4,
        WriterReassigned = 5,
        WriterStartedWork = 6,
        MessageSent = 7,
        FileUploaded = 8,
        DraftSubmitted = 9,
        RevisionRequested = 10,
        RevisionSubmitted = 11,
        OrderCompleted = 12,
        ClientApproved = 13,
        RefundRequested = 14,
        RefundApproved = 15,
        RefundRejected = 16,
        PayoutRequested = 17,
        PayoutProcessed = 18,
        AdminAction = 19,
        SystemAction = 20,
        DeadlineWarning = 21,
        OrderOverdue = 22,
        DeadlineApproaching = 23
    }
}