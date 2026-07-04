namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Describes the type of academic support requested.
    /// Determines whether the client must upload their own original work (draft)
    /// before the order can leave Draft status.
    /// </summary>
    public enum RequestType
    {
        /// <summary>
        /// Client has written a draft and wants feedback/markup.
        /// Requires a StudentDraft attachment.
        /// </summary>
        DraftFeedback = 0,

        /// <summary>
        /// Client wants a concept, topic, or citation style explained.
        /// No draft required.
        /// </summary>
        ConceptExplanation = 1,

        /// <summary>
        /// Client wants grammar/clarity/citation proofreading on a completed piece they wrote.
        /// Requires a StudentDraft attachment.
        /// </summary>
        ProofreadingOwnWork = 2,

        /// <summary>
        /// Client wants a scheduled explanation/Q&A session.
        /// No file required.
        /// </summary>
        LiveTutoringSession = 3
    }
}