using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ScholarRescue.Data;
using ScholarRescue.Data.Seed;
using ScholarRescue.Hubs;
using ScholarRescue.Models;
using ScholarRescue.Services;
using ScholarRescue.Models.Configuration;
using ScholarRescue.Middleware;
using ScholarRescue.Services.Matching;
using ScholarRescue.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

// Enable Npgsql legacy timestamp behavior to handle DateTime.Kind=Unspecified
// This is required because some DateTime values come from model binding (form POST)
// with Kind=Unspecified, and PostgreSQL's 'timestamp with time zone' column requires
// DateTime.Kind=Utc. This switch tells Npgsql to treat unspecified timestamps as UTC.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ============================================================
// CONFIGURATION — deterministic loading order
// ============================================================
// 1. appsettings.json (base, no secrets)
// 2. appsettings.{Environment}.json (environment-specific, no secrets)
// 3. Environment Variables (the ONLY place for secrets)
//
// This is the default ASP.NET Core order, made explicit here
// to prevent any accidental reordering or duplicate loading.
// ============================================================
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// ============================================================
// STARTUP LOGGING — print configuration sources
// ============================================================
var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});
var startupLogger = loggerFactory.CreateLogger("Startup");

startupLogger.LogInformation("========================================");
startupLogger.LogInformation("SCHOLARRESCUE STARTUP");
startupLogger.LogInformation("========================================");
startupLogger.LogInformation("ASPNETCORE_ENVIRONMENT: {Env}", builder.Environment.EnvironmentName);
startupLogger.LogInformation("Content Root: {Root}", builder.Environment.ContentRootPath);

// Log which config files were found
var baseConfigPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
var envConfigPath = Path.Combine(builder.Environment.ContentRootPath, $"appsettings.{builder.Environment.EnvironmentName}.json");
startupLogger.LogInformation("appsettings.json exists: {Exists}", File.Exists(baseConfigPath));
startupLogger.LogInformation("appsettings.{Env}.json exists: {Exists}",
    builder.Environment.EnvironmentName, File.Exists(envConfigPath));

// ============================================================
// CONNECTION STRING VALIDATION
// ============================================================
var cs = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(cs))
{
    startupLogger.LogCritical("FATAL: Connection string 'DefaultConnection' is not configured.");
    startupLogger.LogCritical("Set environment variable: ConnectionStrings__DefaultConnection");
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. " +
        "Set environment variable ConnectionStrings__DefaultConnection.");
}

if (cs.Contains("${PROD_DB_PASSWORD}"))
{
    startupLogger.LogCritical("FATAL: Connection string contains unresolved placeholder '${PROD_DB_PASSWORD}'.");
    throw new InvalidOperationException(
        "Connection string contains unresolved placeholder '${PROD_DB_PASSWORD}'. " +
        "Set environment variable ConnectionStrings__DefaultConnection with the actual password.");
}

// Parse and log connection string details (mask password)
try
{
    var csb = new Npgsql.NpgsqlConnectionStringBuilder(cs);
    startupLogger.LogInformation("Database Host: {Host}", csb.Host);
    startupLogger.LogInformation("Database Port: {Port}", csb.Port);
    startupLogger.LogInformation("Database Name: {Database}", csb.Database);
    startupLogger.LogInformation("Database User: {Username}", csb.Username);
    startupLogger.LogInformation("Database Password: {Password}",
        string.IsNullOrEmpty(csb.Password) ? "(not set)" : "********");
}
catch (Exception ex)
{
    startupLogger.LogWarning("Could not parse connection string: {Error}", ex.Message);
}

startupLogger.LogInformation("========================================");

// ============================================================
// SERVICE REGISTRATION
// ============================================================

// Add services to the container.
builder.Services.AddControllersWithViews();

// Response Compression (Brotli/Gzip) - reduces payload size by 60-80%
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Response Caching for public pages
builder.Services.AddResponseCaching();

// In-Memory Cache for high-performance data retrieval
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// SignalR
builder.Services.AddSignalR();

// Maintenance mode configuration
builder.Services.Configure<MaintenanceModeSettings>(
    builder.Configuration.GetSection("MaintenanceMode"));

// Paystack payment gateway
builder.Services.Configure<PaystackSettings>(
    builder.Configuration.GetSection("Paystack"));
builder.Services.Configure<CurrencySettings>(
    builder.Configuration.GetSection("Currency"));
builder.Services.AddScoped<IPaystackPaymentService, PaystackPaymentService>();
builder.Services.AddHttpClient("Paystack");

// Application services
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddSingleton<IUserPresenceService, UserPresenceService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddSingleton<IPayoutWindowService, PayoutWindowService>();
builder.Services.AddScoped<IWriterApplicationService, WriterApplicationService>();
builder.Services.AddScoped<IOrderAssignmentService, OrderAssignmentService>();
builder.Services.AddScoped<IWorkDeliveryService, WorkDeliveryService>();
builder.Services.AddScoped<IWriterResourceService, WriterResourceService>();
builder.Services.AddScoped<IWriterRankingService, WriterRankingService>();
builder.Services.AddScoped<IOrderMilestoneService, OrderMilestoneService>();
builder.Services.AddScoped<ISupportTicketService, SupportTicketService>();
builder.Services.AddScoped<IOrderMonitoringService, OrderMonitoringService>();
builder.Services.AddHostedService<OrderMonitoringBackgroundService>();
builder.Services.AddHostedService<DeadlineNotificationHostedService>();
builder.Services.AddScoped<IWriterCapacityService, WriterCapacityService>();
builder.Services.AddScoped<IOrderTimelineService, OrderTimelineService>();
builder.Services.AddScoped<IWriterRatingService, WriterRatingService>();
builder.Services.AddScoped<IWriterReliabilityService, WriterReliabilityService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IRevisionDisputeService, RevisionDisputeService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IRiskDetectionService, RiskDetectionService>();
builder.Services.AddScoped<IFileScanningService, FileScanningService>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddHostedService<FileScanHostedService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IHealthMonitorService, HealthMonitorService>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<INotificationQueueService, NotificationQueueService>();
builder.Services.AddScoped<ISecureFileService, SecureFileService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IWriterQualityService, WriterQualityService>();
builder.Services.AddScoped<IWriterTierService, WriterTierService>();
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<ILoginSecurityService, LoginSecurityService>();
builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();
builder.Services.AddScoped<IWriterMatchingService, WriterMatchingService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IAccountFraudService, AccountFraudService>();
builder.Services.AddScoped<IDeploymentValidator, DeploymentValidator>();

// PostgreSQL Database Connection — single source of truth
builder.Services.AddDbContext<ScholarRescueDbContext>(options =>
    options.UseNpgsql(cs)
           .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// ASP.NET Core Identity
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ScholarRescueDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// ============================================================
// STARTUP SEQUENCE
// ============================================================
// 1. Apply all pending EF Core migrations
// 2. Verify database schema and configuration (DeploymentValidator)
// 3. Seed Identity roles
// 4. Seed initial admin user
// 5. Seed application data (writer resources)
// 6. Start web application
//
// WARNING: Database.Migrate() MUST run BEFORE any seeder or
// validator that queries the database. The production database
// may be far behind the code's migration history.
// ============================================================

// ── STEP 1: Apply pending EF Core migrations ─────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ScholarRescueDbContext>();
    startupLogger.LogInformation("Applying pending EF Core migrations...");
    await dbContext.Database.MigrateAsync();
    startupLogger.LogInformation("EF Core migrations applied successfully.");
}

// ── STEP 2: Verify database schema and configuration ──────────
using (var scope = app.Services.CreateScope())
{
    var validator = scope.ServiceProvider.GetRequiredService<IDeploymentValidator>();
    await validator.ValidateAsync();
}

// ── STEP 3-5: Seed data (only after schema is fully migrated) ─
using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    await RoleSeeder.SeedRolesAsync(roleManager);
    await AdminUserSeeder.SeedAdminUserAsync(userManager, roleManager);

    var dbContext = scope.ServiceProvider.GetRequiredService<ScholarRescueDbContext>();
    await WriterResourceSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseResponseCompression();

app.UseHttpsRedirection();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();

app.UseAuthorization();

// Maintenance mode middleware — blocks non-admin traffic when enabled
app.UseMaintenanceMode();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// SignalR Hub endpoints
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<CommunicationHub>("/communicationHub");

app.Run();