namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Status of a revision request made by a client.
    /// </summary>
    public enum RevisionRequestStatus
    {
        /// <summary>
        /// Revision has been requested but not yet completed.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Revision has been completed by the writer.
        /// </summary>
        Completed = 1
    }
}