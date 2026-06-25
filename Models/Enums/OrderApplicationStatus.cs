namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Status of a writer's application to a specific order.
    /// </summary>
    public enum OrderApplicationStatus
    {
        /// <summary>
        /// Writer has applied but no decision has been made yet.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Writer has been selected for the order (order is now assigned to them).
        /// </summary>
        Selected = 1,

        /// <summary>
        /// Application was declined by the administrator.
        /// </summary>
        Declined = 2,

        /// <summary>
        /// Writer voluntarily withdrew their application.
        /// </summary>
        Withdrawn = 3
    }
}
