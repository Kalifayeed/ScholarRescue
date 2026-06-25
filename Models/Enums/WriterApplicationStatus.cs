namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Status of a writer application in the approval workflow.
    /// </summary>
    public enum WriterApplicationStatus
    {
        /// <summary>
        /// Application has been submitted but not yet reviewed.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Application has been approved; user is granted the Writer role.
        /// </summary>
        Approved = 1,

        /// <summary>
        /// Application has been rejected; user is informed with a reason.
        /// </summary>
        Rejected = 2,

        /// <summary>
        /// Administrator has requested additional information from the applicant.
        /// </summary>
        MoreInformationRequired = 3,

        /// <summary>
        /// Approved writer has been suspended by an administrator.
        /// Writer cannot access the platform until reactivated.
        /// </summary>
        Suspended = 4
    }
}
