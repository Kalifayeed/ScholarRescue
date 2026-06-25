using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    public interface IRevisionDisputeService
    {
        // --- Revision Management ---
        Task<Models.RevisionRequest> RequestRevisionAsync(int orderId, string clientId, string title, string description);
        Task<Models.RevisionRequest> SubmitRevisionAsync(int revisionId, string writerId);
        Task<Models.RevisionRequest> ApproveRevisionAsync(int revisionId, string clientId);
        Task<List<Models.RevisionRequest>> GetOrderRevisionsAsync(int orderId);

        // --- Dispute Management ---
        Task<OrderDispute> OpenDisputeAsync(int orderId, string clientId, string title, string description, string disputeType);
        Task<OrderDispute> ResolveDisputeAsync(int disputeId, string adminId, string resolution, string decision);
        Task<OrderDispute?> GetDisputeAsync(int orderId);
        Task<List<OrderDispute>> GetAllDisputesAsync(string? status = null);

        // --- Evidence ---
        Task<DisputeEvidence> AddEvidenceAsync(int disputeId, string uploadedBy, string fileName, string filePath, string? description);
        Task<List<DisputeEvidence>> GetEvidenceAsync(int disputeId);
    }
}