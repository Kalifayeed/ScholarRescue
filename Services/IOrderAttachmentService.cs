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
        /// Validates a list of uploaded files against extension, size, and count rules.
        /// Throws <see cref="InvalidOperationException"/> on the first failure.
        /// Use this before creating any database records to avoid partial persistence.
        /// </summary>
        /// <param name="files">The uploaded file data from the form.</param>
        void ValidateFiles(List<IFormFile> files);

        /// <summary>
        /// Assumes <see cref="ValidateFiles"/> has already passed (or no files provided).
        /// Saves to disk and creates <see cref="OrderAttachment"/> rows.
        /// Returns the list of created attachments.
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