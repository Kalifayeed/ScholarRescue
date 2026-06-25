namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Represents the type of submission made by a writer.
    /// </summary>
    public enum SubmissionType
    {
        /// <summary>
        /// Initial draft submission.
        /// </summary>
        Draft = 0,

        /// <summary>
        /// Revision based on client feedback.
        /// </summary>
        Revision = 1,

        /// <summary>
        /// Final accepted version.
        /// </summary>
        Final = 2
    }
}