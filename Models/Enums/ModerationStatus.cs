namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Moderation status for uploaded files.
    /// </summary>
    public enum ModerationStatus
    {
        Pending = 0,
        Scanning = 1,
        Approved = 2,
        Flagged = 3,
        Blocked = 4,
        Quarantined = 5
    }
}