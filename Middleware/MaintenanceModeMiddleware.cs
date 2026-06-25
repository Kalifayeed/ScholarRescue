using System.Text;
using Microsoft.Extensions.Options;
using ScholarRescue.Models.Configuration;

namespace ScholarRescue.Middleware
{
    /// <summary>
    /// Middleware that blocks non-admin user access when maintenance mode is enabled.
    /// Allows admins through so they can perform upgrades and verify the system.
    /// Bypasses webhooks and health check endpoints automatically.
    /// </summary>
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MaintenanceModeSettings _settings;
        private readonly ILogger<MaintenanceModeMiddleware> _logger;

        // Cache the bypass paths as a HashSet for fast lookup
        private readonly HashSet<string> _bypassPaths;

        public MaintenanceModeMiddleware(
            RequestDelegate next,
            IOptions<MaintenanceModeSettings> settings,
            ILogger<MaintenanceModeMiddleware> logger)
        {
            _next = next;
            _settings = settings.Value;
            _logger = logger;

            // Parse bypass paths from comma-separated config value
            _bypassPaths = (_settings.BypassPaths ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.TrimStart('/').ToLowerInvariant())
                .ToHashSet();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // If maintenance mode is not enabled, proceed normally
            if (!_settings.Enabled)
            {
                await _next(context);
                return;
            }

            // Check if the current request path should bypass maintenance mode
            var requestPath = context.Request.Path.Value?.TrimStart('/').ToLowerInvariant() ?? "";
            if (_bypassPaths.Contains(requestPath))
            {
                await _next(context);
                return;
            }

            // Check if the user is an authenticated admin
            if (_settings.AllowAdminAccess && context.User?.Identity?.IsAuthenticated == true)
            {
                if (context.User.IsInRole("Administrator"))
                {
                    await _next(context);
                    return;
                }
            }

            // Log the blocked request
            _logger.LogInformation(
                "Maintenance mode: Blocked request to {Path} from user {User}",
                context.Request.Path,
                context.User?.Identity?.Name ?? "anonymous");

            // Set the response status code
            context.Response.StatusCode = _settings.StatusCode;
            context.Response.ContentType = "text/html; charset=utf-8";

            // Render the maintenance page
            var html = BuildMaintenancePage();
            await context.Response.WriteAsync(html, Encoding.UTF8);
        }

        private string BuildMaintenancePage()
        {
            var estimatedReturnHtml = string.IsNullOrEmpty(_settings.EstimatedReturnTime)
                ? ""
                : $"<p class=\"estimated-return\">Estimated return: <strong>{HtmlEncode(_settings.EstimatedReturnTime)}</strong></p>";

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Scheduled Maintenance – ScholarRescue</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
            color: #e0e0e0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .maintenance-container {{
            max-width: 600px;
            width: 100%;
            text-align: center;
            padding: 40px 30px;
            background: rgba(255, 255, 255, 0.05);
            border-radius: 16px;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.1);
        }}
        .icon {{
            font-size: 64px;
            margin-bottom: 20px;
            display: block;
        }}
        h1 {{
            font-size: 28px;
            font-weight: 700;
            color: #ffffff;
            margin-bottom: 16px;
        }}
        p {{
            font-size: 16px;
            line-height: 1.6;
            color: #b0b0b0;
            margin-bottom: 12px;
        }}
        .estimated-return {{
            margin-top: 20px;
            padding: 12px 20px;
            background: rgba(255, 193, 7, 0.1);
            border: 1px solid rgba(255, 193, 7, 0.3);
            border-radius: 8px;
            color: #ffc107;
            font-size: 14px;
        }}
        .brand {{
            margin-top: 30px;
            font-size: 14px;
            color: #666;
        }}
        .brand strong {{
            color: #888;
        }}
    </style>
</head>
<body>
    <div class=""maintenance-container"">
        <span class=""icon"">🔧</span>
        <h1>Scheduled Maintenance</h1>
        <p>{HtmlEncode(_settings.DisplayMessage)}</p>
        {estimatedReturnHtml}
        <p style=""margin-top: 24px; font-size: 14px;"">
            We appreciate your patience. For urgent inquiries, please contact support.
        </p>
        <div class=""brand"">
            <strong>ScholarRescue</strong> &mdash; Academic Support Platform
        </div>
    </div>
</body>
</html>";
        }

        private static string HtmlEncode(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value ?? "");
        }
    }

    /// <summary>
    /// Extension method to register the maintenance mode middleware in the pipeline.
    /// </summary>
    public static class MaintenanceModeMiddlewareExtensions
    {
        public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaintenanceModeMiddleware>();
        }
    }
}