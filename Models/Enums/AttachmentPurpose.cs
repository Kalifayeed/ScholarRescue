namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Describes the purpose of a file attached to an order.
    /// Used to distinguish the client's own original work from supporting materials.
    /// </summary>
    public enum AttachmentPurpose
    {
        /// <summary>
        /// The client's own original work being reviewed (draft, proofreading copy, etc.).
        /// Required for DraftFeedback and ProofreadingOwnWork request types.
        /// </summary>
        StudentDraft = 0,

        /// <summary>
        /// Rubric, prompt, or instructor guidelines (supporting material only,
        /// never the deliverable basis).
        /// </summary>
        AssignmentInstructions = 1,

        /// <summary>
        /// Any other reference file.
        /// </summary>
        SupportingMaterial = 2
    }
}