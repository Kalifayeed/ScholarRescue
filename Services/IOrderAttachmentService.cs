using Microsoft.AspNetCore.Http;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for persisting client-uploaded order attachments (drafts, instructions, etc.).
    /// </summary>
    public interface IOrderAttachmentService
    {
        /// <summary>
        /// Validates, saves to disk, and creates <see cref="OrderAttachment"/> rows.
        /// Validates all files upfront before writing any — no partial saves.
        /// Returns the list of created attachments. Throws on validation failure.
        /// </summary>
        /// <param name="orderId">The order to attach files to.</param>
        /// <param name="files">The uploaded file data from the form.</param>
        /// <param name="purpose">
        /// The purpose to assign. For DraftFeedback/ProofreadingOwnWork, pass StudentDraft;
        /// otherwise SupportingMaterial.
        /// </param>
        /// <param name="uploadedById">User ID of the uploader.</param>
        /// <returns>The list of persisted <see cref="OrderAttachment"/> records.</returns>
        Task<List<OrderAttachment>> SaveAttachmentsAsync(
            int orderId,
            List<IFormFile> files,
            AttachmentPurpose purpose,
            string uploadedById);
    }
}