using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Service for managing email templates and rendering transactional emails.
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>Renders an email template by key, replacing placeholders.</summary>
        Task<(string Subject, string HtmlBody)> RenderTemplateAsync(string templateKey, Dictionary<string, string> placeholders);

        /// <summary>Gets a template by key.</summary>
        Task<EmailTemplate?> GetTemplateAsync(string templateKey);

        /// <summary>Creates or updates a template.</summary>
        Task SaveTemplateAsync(string templateKey, string subject, string htmlContent);

        /// <summary>Gets all templates.</summary>
        Task<List<EmailTemplate>> GetAllTemplatesAsync();
    }
}