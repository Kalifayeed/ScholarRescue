namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Defines the auto-assignment mode for the Intelligent Writer Matching Engine.
    /// Controls how the system handles writer recommendations and assignments.
    /// </summary>
    public enum AutoAssignmentMode
    {
        /// <summary>
        /// Auto-assignment is completely disabled. Only manual admin assignment.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// System generates recommendations but never auto-assigns. Default mode.
        /// </summary>
        RecommendationOnly = 1,

        /// <summary>
        /// System automatically assigns the highest-ranked eligible writer when requirements are met.
        /// </summary>
        AutomaticAssignment = 2
    }
}