using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service interface for the writer work delivery workflow.
    /// Handles submissions, revisions, client acceptance, and financial integration.
    /// </summary>
    public interface IWorkDeliveryService
    {
        /// <summary>
        /// Writer uploads a work submission (draft/revision/final) for an order.
        /// </summary>
        Task<OrderSubmission> SubmitWorkAsync(int orderId, string writerId, IFormFile file, string comments, SubmissionType submissionType);

        /// <summary>
        /// Gets all submissions for a specific order.
        /// </summary>
        Task<List<OrderSubmission>> GetSubmissionsAsync(int orderId);

        /// <summary>
        /// Gets the submission timeline for an order (ordered by version).
        /// </summary>
        Task<List<OrderSubmission>> GetSubmissionTimelineAsync(int orderId);

        /// <summary>
        /// Client requests a revision on submitted work.
        /// </summary>
        Task<RevisionRequest> RequestRevisionAsync(int orderId, string clientId, string comments);

        /// <summary>
        /// Gets all pending revision requests for a writer.
        /// </summary>
        Task<List<RevisionRequest>> GetPendingRevisionsAsync(string writerId);

        /// <summary>
        /// Gets all revision requests for an order.
        /// </summary>
        Task<List<RevisionRequest>> GetOrderRevisionsAsync(int orderId);

        /// <summary>
        /// Client accepts the work and completes the order.
        /// Triggers accounting: commission deduction, writer wallet credit.
        /// </summary>
        Task AcceptWorkAsync(int orderId, string clientId);

        /// <summary>
        /// Admin overrides: forces order completion with accounting.
        /// </summary>
        Task AdminForceCompletionAsync(int orderId, string adminId);

        /// <summary>
        /// Admin forces a revision cycle on the order.
        /// </summary>
        Task AdminForceRevisionAsync(int orderId, string adminId, string comments);

        /// <summary>
        /// Validates file type for uploads (PDF, DOC, DOCX, ZIP).
        /// </summary>
        bool IsValidFileType(IFormFile file);
    }
}