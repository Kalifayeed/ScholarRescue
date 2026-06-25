using System.Text;
using Npgsql;
using ScholarRescue.Models.Configuration;

namespace ScholarRescue.Services;

public class DeploymentValidator : IDeploymentValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeploymentValidator> _logger;
    private readonly IWebHostEnvironment _env;

    public DeploymentValidator(
        IConfiguration configuration,
        ILogger<DeploymentValidator> logger,
        IWebHostEnvironment env)
    {
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    public async Task ValidateAsync()
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var info = new List<string>();
        var sb = new StringBuilder();

        void LogInfo(string label, string status) => info.Add($"  {label,-30} {status}");
        void LogWarn(string label, string status) => warnings.Add($"  {label,-30} {status}");
        void LogError(string label, string status) => errors.Add($"  {label,-30} {status}");

        var envName = _configuration["ASPNETCORE_ENVIRONMENT"] ?? _env.EnvironmentName;
        var contentRoot = _env.ContentRootPath;
        var webRoot = _env.WebRootPath ?? Path.Combine(contentRoot, "wwwroot");

        // ── Configuration Sources ──────────────────────────────
        // We log the environment, which determines which config files are loaded
        // ── Platform Settings ──────────────────────────────────
        var baseUrl = _configuration["Platform:BaseUrl"] ?? "";
        var supportEmail = _configuration["Platform:SupportEmail"] ?? "";
        var platformName = _configuration["Platform:Name"] ?? "";
        var appVersion = _configuration["Platform:Version"] ?? "1.0.0";

        if (string.IsNullOrWhiteSpace(baseUrl))   LogError("Platform:BaseUrl", "MISSING");
        else LogInfo("Platform:BaseUrl", baseUrl);

        if (string.IsNullOrWhiteSpace(supportEmail)) LogError("Platform:SupportEmail", "MISSING");
        else LogInfo("Platform:SupportEmail", supportEmail);

        if (string.IsNullOrWhiteSpace(platformName)) LogError("Platform:Name", "MISSING");
        else LogInfo("Platform:Name", platformName);

        // Validate domain for production
        if (envName.Equals("Production", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(baseUrl))
        {
            var expectedDomain = "https://scholar-rescue.com".TrimEnd('/');
            var actualDomain = baseUrl.TrimEnd('/');
            if (!actualDomain.Equals(expectedDomain, StringComparison.OrdinalIgnoreCase))
            {
                LogError("BaseUrl Domain", $"Expected '{expectedDomain}', got '{actualDomain}'");
            }
            else
            {
                LogInfo("BaseUrl Domain", "verified");
            }
        }

        // ── Database ───────────────────────────────────────────
        var cs = _configuration.GetConnectionString("DefaultConnection");
        string dbHost = "(not set)", dbName = "(not set)", dbUser = "(not set)", dbPass = "(not set)";

        if (string.IsNullOrEmpty(cs))
        {
            LogError("Database", "Connection string missing");
            errors.Add("  FATAL: ConnectionStrings__DefaultConnection is not configured");
        }
        else if (cs.Contains("${PROD_DB_PASSWORD}"))
        {
            LogError("Database", "Unresolved placeholder ${PROD_DB_PASSWORD}");
            errors.Add("  FATAL: Connection string contains unresolved placeholder");
        }
        else
        {
            try
            {
                var csb = new NpgsqlConnectionStringBuilder(cs);
                dbHost = csb.Host ?? "localhost";
                dbName = csb.Database ?? "";
                dbUser = csb.Username ?? "";
                dbPass = string.IsNullOrEmpty(csb.Password) ? "(not set)" : "********";

                LogInfo("Database Host", dbHost);
                LogInfo("Database Name", dbName);
                LogInfo("Database User", dbUser);
                LogInfo("Database Password", dbPass);

                // If using postgres user, warn but don't fail
                if ("postgres".Equals(dbUser, StringComparison.OrdinalIgnoreCase))
                {
                    LogWarn("Database User", "Using 'postgres' superuser - consider a dedicated user");
                }

                // Test connectivity
                _logger.LogInformation("Testing database connectivity...");
                using var conn = new NpgsqlConnection(cs);
                await conn.OpenAsync();
                LogInfo("Database Connected", "YES");
                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                LogError("Database Connected", "NO");
                errors.Add($"  Database connection failed: {ex.Message}");
            }
        }

        // ── Redis ──────────────────────────────────────────────
        var redisConnStr = _configuration.GetConnectionString("Redis")
            ?? _configuration["Redis:ConnectionString"]
            ?? "";
        if (string.IsNullOrWhiteSpace(redisConnStr))
        {
            LogWarn("Redis", "Not Configured");
        }
        else
        {
            try
            {
                // We just validate the config exists; actual Redis test would need StackExchange.Redis
                LogInfo("Redis", "Configured");
            }
            catch
            {
                LogError("Redis", "Connection failed");
            }
        }

        // ── SMTP / SendGrid ───────────────────────────────────
        var smtpHost = _configuration["Email:SmtpHost"] ?? "";
        var smtpUser = _configuration["Email:SmtpUsername"] ?? _configuration["Email:Username"] ?? "";
        var smtpPass = _configuration["Email:SmtpPassword"] ?? _configuration["Email:Password"] ?? "";
        var fromAddress = _configuration["Email:FromAddress"] ?? "";

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpPass))
        {
            LogWarn("SMTP/SendGrid", "Not Configured");
        }
        else
        {
            LogInfo("SMTP Host", smtpHost);
            LogInfo("SMTP From", fromAddress);
            LogInfo("SMTP Key", string.IsNullOrEmpty(smtpPass) ? "MISSING" : "********");
        }

        // ── Stripe ─────────────────────────────────────────────
        var stripePubKey = _configuration["Stripe:PublishableKey"] ?? _configuration["Stripe:PublishableKey"] ?? "";
        var stripeSecKey = _configuration["Stripe:SecretKey"] ?? _configuration["Stripe:SecretKey"] ?? "";

        if (string.IsNullOrWhiteSpace(stripePubKey) && string.IsNullOrWhiteSpace(stripeSecKey))
        {
            LogWarn("Stripe", "Not Configured");
        }
        else
        {
            LogInfo("Stripe PublishableKey",
                string.IsNullOrEmpty(stripePubKey) ? "MISSING" : "********");
            LogInfo("Stripe SecretKey",
                string.IsNullOrEmpty(stripeSecKey) ? "MISSING" : "********");
        }

        // ── Paystack ───────────────────────────────────────────
        var paystackPubKey = _configuration["Paystack:PublicKey"] ?? "";
        var paystackSecKey = _configuration["Paystack:SecretKey"] ?? "";

        if (string.IsNullOrWhiteSpace(paystackPubKey) && string.IsNullOrWhiteSpace(paystackSecKey))
        {
            LogWarn("Paystack", "Not Configured");
        }
        else
        {
            LogInfo("Paystack PublicKey",
                string.IsNullOrEmpty(paystackPubKey) ? "MISSING" : "********");
            LogInfo("Paystack SecretKey",
                string.IsNullOrEmpty(paystackSecKey) ? "MISSING" : "********");
        }

        // ── Upload Directories ─────────────────────────────────
        var uploadDirs = new[]
        {
            Path.Combine(webRoot, "uploads"),
            Path.Combine(webRoot, "uploads", "orders"),
            Path.Combine(webRoot, "uploads", "messages"),
            Path.Combine(webRoot, "uploads", "profiles"),
            Path.Combine(webRoot, "uploads", "documents"),
        };

        foreach (var dir in uploadDirs)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    LogInfo($"Created directory", dir);
                }
                else
                {
                    LogInfo($"Directory exists", dir);
                }

                // Test writability
                var testFile = Path.Combine(dir, ".write_test");
                File.WriteAllText(testFile, "ok");
                File.Delete(testFile);
            }
            catch (Exception ex)
            {
                LogError($"Directory write check", dir);
                errors.Add($"  Cannot write to {dir}: {ex.Message}");
            }
        }

        // ── Logs Directory ─────────────────────────────────────
        var logsDir = Path.Combine(contentRoot, "Logs");
        try
        {
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
                LogInfo("Created directory", logsDir);
            }
            LogInfo("Logs directory", logsDir);
        }
        catch (Exception ex)
        {
            LogError("Logs directory", ex.Message);
        }

        // ── HTTPS Check ────────────────────────────────────────
        var httpsEnabled = _configuration["ASPNETCORE_URLS"]?.Contains("https", StringComparison.OrdinalIgnoreCase) ?? false;
        if (envName.Equals("Production", StringComparison.OrdinalIgnoreCase) && !httpsEnabled)
        {
            LogWarn("HTTPS", "Not configured on ASPNETCORE_URLS (check Nginx reverse proxy)");
        }
        else
        {
            LogInfo("HTTPS", httpsEnabled ? "Enabled" : "Behind reverse proxy");
        }

        // ── SignalR ────────────────────────────────────────────
        LogInfo("SignalR Hubs", "/chatHub, /notificationHub, /communicationHub");

        // ── Static Files ───────────────────────────────────────
        var staticDir = webRoot;
        if (Directory.Exists(staticDir))
        {
            LogInfo("Static Files", staticDir);
        }
        else
        {
            LogError("Static Files", $"Directory not found: {staticDir}");
        }

        // ════════════════════════════════════════════════════════
        // REPORT
        // ════════════════════════════════════════════════════════
        _logger.LogInformation("");
        _logger.LogInformation("======================================");
        _logger.LogInformation("ScholarRescue Deployment Verification");
        _logger.LogInformation("======================================");
        _logger.LogInformation("Application Version: {Version}", appVersion);
        _logger.LogInformation("Environment:         {Env}", envName);
        _logger.LogInformation("Content Root:        {Root}", contentRoot);
        _logger.LogInformation("Current Domain:      {Url}", baseUrl);
        _logger.LogInformation("");

        _logger.LogInformation("Configuration Sources:");
        _logger.LogInformation("  - appsettings.json");
        _logger.LogInformation("  - appsettings.{Env}.json", envName);
        _logger.LogInformation("  - Environment Variables");

        _logger.LogInformation("");
        _logger.LogInformation("Database");
        _logger.LogInformation("--------");
        _logger.LogInformation("Host:          {Host}", dbHost);
        _logger.LogInformation("Database:      {Name}", dbName);
        _logger.LogInformation("Username:      {User}", dbUser);
        _logger.LogInformation("Password:      {Pass}", dbPass);

        _logger.LogInformation("");
        _logger.LogInformation("Services");
        _logger.LogInformation("--------");
        foreach (var i in info) _logger.LogInformation(i);
        foreach (var w in warnings) _logger.LogWarning(w);
        foreach (var e in errors) _logger.LogError(e);

        _logger.LogInformation("");

        // Final verdict
        if (errors.Count > 0)
        {
            var fatalErrors = errors.Where(e => e.StartsWith("  FATAL:")).ToList();

            _logger.LogCritical("======================================");
            _logger.LogCritical("DEPLOYMENT VERIFICATION: FAILED");
            _logger.LogCritical("======================================");

            if (fatalErrors.Count > 0)
            {
                _logger.LogCritical("Fatal configuration errors detected:");
                foreach (var fe in fatalErrors)
                    _logger.LogCritical(fe);

                _logger.LogCritical("Startup aborted. Fix the errors above and restart.");
                throw new InvalidOperationException(
                    $"Deployment verification failed with {errors.Count} error(s).\n" +
                    string.Join("\n", errors));
            }

            _logger.LogWarning("Non-fatal configuration warnings exist. Application will start.");
        }
        else
        {
            _logger.LogInformation("======================================");
            _logger.LogInformation("DEPLOYMENT VERIFICATION: PASSED");
            _logger.LogInformation("======================================");
        }
    }
}