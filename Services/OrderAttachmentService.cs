using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Validates, saves to disk, and persists <see cref="OrderAttachment"/> records
    /// for client-uploaded files during order creation.
    /// </summary>
    public class OrderAttachmentService : IOrderAttachmentService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<OrderAttachmentService> _logger;

        public OrderAttachmentService(
            ScholarRescueDbContext context,
            ILogger<OrderAttachmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<OrderAttachment>> SaveAttachmentsAsync(
            int orderId,
            List<IFormFile> files,
            AttachmentPurpose purpose,
            string uploadedById)
        {
            if (files == null || files.Count == 0)
                return new List<OrderAttachment>();

            // Validate all files upfront before writing anything
            ValidateFiles(files);

            var attachments = new List<OrderAttachment>();
            var uploadDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "orders",
                orderId.ToString(),
                "attachments");
            Directory.CreateDirectory(uploadDir);

            // Write all files concurrently
            var saveTasks = files.Select(async file =>
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadDir, storedName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var attachment = new OrderAttachment
                {
                    OrderId = orderId,
                    FileName = file.FileName,
                    StoredFileName = storedName,
                    FilePath = $"/uploads/orders/{orderId}/attachments/{storedName}",
                    FileSize = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    UploadedById = uploadedById,
                    AttachmentPurpose = purpose
                };

                lock (attachments)
                {
                    attachments.Add(attachment);
                }
            });

            await Task.WhenAll(saveTasks);

            _context.OrderAttachments.AddRange(attachments);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Saved {Count} attachment(s) for order {OrderId} with purpose {Purpose}.",
                attachments.Count, orderId, purpose);

            return attachments;
        }

        /// <summary>
        /// Validates all files: extension, size, and total count.
        /// Throws <see cref="InvalidOperationException"/> on first failure,
        /// identifying which file failed and why.
        /// </summary>
        private static void ValidateFiles(List<IFormFile> files)
        {
            if (files.Count > OrderAttachmentValidation.MaxFilesPerOrder)
            {
                throw new InvalidOperationException(
                    $"You can upload a maximum of {OrderAttachmentValidation.MaxFilesPerOrder} files per order.");
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(ext) ||
                    !OrderAttachmentValidation.AllowedExtensions.Contains(ext))
                {
                    throw new InvalidOperationException(
                        $"File \"{file.FileName}\" has an unsupported file type " +
                        $"({ext}). Allowed types: " +
                        string.Join(", ", OrderAttachmentValidation.AllowedExtensions));
                }

                if (file.Length > OrderAttachmentValidation.MaxFileSizeBytes)
                {
                    throw new InvalidOperationException(
                        $"File \"{file.FileName}\" exceeds the maximum size of " +
                        $"{OrderAttachmentValidation.MaxFileSizeBytes / (1024 * 1024)} MB.");
                }

                if (file.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"File \"{file.FileName}\" is empty.");
                }
            }
        }
    }
}