using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

// PostgreSQL Database Connection
builder.Services.AddDbContext<ScholarRescueDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Seed Roles and Default Admin User on application startup
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

app.Run();