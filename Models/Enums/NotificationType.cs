namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Represents the type/category of a notification event.
    /// Designed for extensibility to support future notification channels.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>An order has been assigned to a writer.</summary>
        OrderAssigned = 0,

        /// <summary>A new message has been received.</summary>
        NewMessage = 1,

        /// <summary>A revision has been requested on an order.</summary>
        RevisionRequested = 2,

        /// <summary>An order has been submitted by a writer.</summary>
        OrderSubmitted = 3,

        /// <summary>An order has been completed.</summary>
        OrderCompleted = 4,

        /// <summary>A writer application has been approved.</summary>
        WriterApproved = 5,

        /// <summary>A system-wide alert or announcement.</summary>
        SystemAlert = 6,

        /// <summary>A writer has applied to an order in the marketplace.</summary>
        WriterApplied = 7,

        /// <summary>A writer was selected for an order (same as WriterAssigned).</summary>
        WriterAssigned = 8,

        /// <summary>A writer was rejected/declined for an order.</summary>
        WriterRejected = 9,

        /// <summary>An order was reassigned to the marketplace.</summary>
        OrderReassigned = 10,

        /// <summary>A writer application was rejected by admin.</summary>
        WriterApplicationRejected = 11,

        /// <summary>General notification with no specific category.</summary>
        General = 12,

        /// <summary>A new order has been created (admin notification).</summary>
        NewOrder = 13,

        /// <summary>A file has been uploaded to an order.</summary>
        FileUploaded = 14,

        /// <summary>A dispute has been opened on an order.</summary>
        DisputeOpened = 15,

        /// <summary>A dispute has been resolved on an order.</summary>
        DisputeResolved = 16,

        /// <summary>A writer has submitted a payout request.</summary>
        PayoutRequested = 17,

        /// <summary>A payout request has been approved.</summary>
        PayoutApproved = 18,

        /// <summary>A payout request has been rejected.</summary>
        PayoutRejected = 19,

        /// <summary>A deadline reminder for an order.</summary>
        DeadlineReminder = 20,

        /// <summary>System notification.</summary>
        System = 21,

        /// <summary>Order payment received and escrow funded.</summary>
        OrderFunded = 22
    }
}