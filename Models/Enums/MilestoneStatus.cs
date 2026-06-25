namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Status of an OrderMilestone in the progressive delivery workflow.
    /// </summary>
    public enum MilestoneStatus
    {
        /// <summary>
        /// Milestone created by admin, awaiting writer submission.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Writer has uploaded files for this milestone.
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// Client has approved the milestone; earnings recorded in ledger.
        /// </summary>
        Approved = 2
    }
}
