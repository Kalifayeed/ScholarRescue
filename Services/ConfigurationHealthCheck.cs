using Npgsql;
using ScholarRescue.Data;

namespace ScholarRescue.Services;

public class ConfigurationHealthCheck : IConfigurationHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationHealthCheck> _logger;

    public ConfigurationHealthCheck(
        IConfiguration configuration,
        ILogger<ConfigurationHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var configFiles = new List<string>();

        // Determine which config files were loaded
        var basePath = Directory.GetCurrentDirectory();
        var baseConfig = Path.Combine(basePath, "appsettings.json");
        var envConfig = Path.Combine(basePath, $"appsettings.{env}.json");

        if (File.Exists(baseConfig)) configFiles.Add(baseConfig);
        if (File.Exists(envConfig)) configFiles.Add(envConfig);

        _logger.LogInformation("========================================");
        _logger.LogInformation("CONFIGURATION HEALTH CHECK");
        _logger.LogInformation("========================================");
        _logger.LogInformation("ASPNETCORE_ENVIRONMENT: {Env}", env);
        _logger.LogInformation("Config files loaded: {Files}",
            configFiles.Count > 0 ? string.Join(", ", configFiles) : "(none)");

        var cs = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(cs))
        {
            _logger.LogCritical("FATAL: Connection string 'DefaultConnection' is not configured.");
            _logger.LogCritical("Set it via environment variable: ConnectionStrings__DefaultConnection");
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Set environment variable ConnectionStrings__DefaultConnection.");
        }

        // Validate forbidden patterns
        if (cs.Contains("Username=postgres", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogCritical("FATAL: Connection string uses 'Username=postgres' which is forbidden in production.");
            throw new InvalidOperationException(
                "Connection string uses 'Username=postgres'. " +
                "Create a dedicated database user instead.");
        }

        if (cs.Contains("${PROD_DB_PASSWORD}"))
        {
            _logger.LogCritical("FATAL: Connection string contains unresolved placeholder '${PROD_DB_PASSWORD}'.");
            throw new InvalidOperationException(
                "Connection string contains unresolved placeholder '${PROD_DB_PASSWORD}'. " +
                "Set environment variable ConnectionStrings__DefaultConnection with the actual password.");
        }

        // Parse and log connection string details (mask password)
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(cs);
            _logger.LogInformation("Database Host: {Host}", builder.Host);
            _logger.LogInformation("Database Port: {Port}", builder.Port);
            _logger.LogInformation("Database Name: {Database}", builder.Database);
            _logger.LogInformation("Database User: {Username}", builder.Username);
            _logger.LogInformation("Database Password: {Password}",
                string.IsNullOrEmpty(builder.Password) ? "(not set)" : "********");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not parse connection string: {Error}", ex.Message);
        }

        // Test database connectivity
        _logger.LogInformation("Testing database connectivity...");
        try
        {
            using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            _logger.LogInformation("Database connection: OK");
            await conn.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection FAILED: {Message}", ex.Message);
            throw new InvalidOperationException(
                $"Cannot connect to the database: {ex.Message}", ex);
        }

        _logger.LogInformation("========================================");
        _logger.LogInformation("CONFIGURATION HEALTH CHECK COMPLETE");
        _logger.LogInformation("========================================");
    }
}