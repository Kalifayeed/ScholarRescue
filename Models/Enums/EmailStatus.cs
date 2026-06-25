namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Status of an email in the queue.
    /// </summary>
    public enum EmailStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3
    }
}