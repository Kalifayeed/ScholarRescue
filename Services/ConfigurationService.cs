using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Platform Configuration Center - manages all business rules, feature flags,
    /// and operational settings from a centralized admin interface.
    /// Settings are cached in memory for fast access without redeployment.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<ConfigurationService> _logger;
        private static Dictionary<string, string> _settingsCache = new();
        private static Dictionary<string, bool> _featureCache = new();
        private static DateTime _lastCacheRefresh = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ConfigurationService(ScholarRescueDbContext context, ILogger<ConfigurationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ----------------------------------------------------------------
        // SETTINGS
        // ----------------------------------------------------------------

        public async Task<string?> GetSettingAsync(string key)
        {
            await EnsureCacheLoadedAsync();
            return _settingsCache.TryGetValue(key, out var value) ? value : null;
        }

        public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
        {
            var value = await GetSettingAsync(key);
            if (value == null) return defaultValue;

            try
            {
                return typeof(T) switch
                {
                    Type t when t == typeof(int) && int.TryParse(value, out var i) => (T)(object)i,
                    Type t when t == typeof(decimal) && decimal.TryParse(value, out var d) => (T)(object)d,
                    Type t when t == typeof(bool) && bool.TryParse(value, out var b) => (T)(object)b,
                    Type t when t == typeof(double) && double.TryParse(value, out var db) => (T)(object)db,
                    _ => JsonSerializer.Deserialize<T>(value) ?? defaultValue
                };
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task SetSettingAsync(string key, string value, string? updatedById = null)
        {
            var setting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == key);
            var oldValue = setting?.Value;

            if (setting == null)
            {
                setting = new PlatformSetting
                {
                    Key = key,
                    Value = value,
                    Category = "General",
                    DataType = InferDataType(value),
                    IsEditable = true,
                    UpdatedById = updatedById,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.PlatformSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedById = updatedById;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Log change
            _context.AuditLogs.Add(new AuditLog
            {
                Action = "Setting Changed",
                PerformedById = updatedById ?? "System",
                Description = $"Setting '{key}' changed from '{oldValue}' to '{value}'",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Invalidate cache
            _lastCacheRefresh = DateTime.MinValue;
        }

        public async Task<List<PlatformSetting>> GetSettingsByCategoryAsync(string category)
        {
            return await _context.PlatformSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();
        }

        public async Task<Dictionary<string, List<PlatformSetting>>> GetAllSettingsAsync()
        {
            var settings = await _context.PlatformSettings
                .OrderBy(s => s.Category).ThenBy(s => s.Key)
                .ToListAsync();
            return settings.GroupBy(s => s.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // ----------------------------------------------------------------
        // FEATURE FLAGS
        // ----------------------------------------------------------------

        public async Task<bool> IsFeatureEnabledAsync(string featureName)
        {
            await EnsureFeatureCacheLoadedAsync();
            return _featureCache.TryGetValue(featureName, out var enabled) && enabled;
        }

        public async Task SetFeatureFlagAsync(string featureName, bool enabled, string? updatedById = null)
        {
            var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.FeatureName == featureName);

            if (flag == null)
            {
                flag = new FeatureFlag
                {
                    FeatureName = featureName,
                    Enabled = enabled,
                    UpdatedById = updatedById,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.FeatureFlags.Add(flag);
            }
            else
            {
                flag.Enabled = enabled;
                flag.UpdatedById = updatedById;
                flag.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                Action = enabled ? "Feature Enabled" : "Feature Disabled",
                PerformedById = updatedById ?? "System",
                Description = $"Feature '{featureName}' {(enabled ? "enabled" : "disabled")}",
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _lastCacheRefresh = DateTime.MinValue;
        }

        public async Task<List<FeatureFlag>> GetAllFeatureFlagsAsync()
        {
            return await _context.FeatureFlags.OrderBy(f => f.FeatureName).ToListAsync();
        }

        // ----------------------------------------------------------------
        // IMPORT / EXPORT
        // ----------------------------------------------------------------

        public async Task<string> ExportSettingsAsync()
        {
            var settings = await _context.PlatformSettings.ToListAsync();
            var data = settings.Select(s => new
            {
                s.Key, s.Value, s.Category, s.DataType, s.Description
            });
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<int> ImportSettingsAsync(string json, string? updatedById = null)
        {
            var imports = JsonSerializer.Deserialize<List<PlatformSetting>>(json);
            if (imports == null) return 0;

            int count = 0;
            foreach (var import in imports)
            {
                await SetSettingAsync(import.Key, import.Value, updatedById);
                count++;
            }
            return count;
        }

        // ----------------------------------------------------------------
        // VALIDATION
        // ----------------------------------------------------------------

        public (bool IsValid, string? Error) ValidateSetting(string key, string value, string dataType)
        {
            return dataType switch
            {
                "Integer" => int.TryParse(value, out _) ? (true, null) : (false, "Must be a valid integer"),
                "Decimal" => decimal.TryParse(value, out var d) && d >= 0 ? (true, null) : (false, "Must be a valid positive decimal"),
                "Boolean" => bool.TryParse(value, out _) ? (true, null) : (false, "Must be true or false"),
                "Percentage" => decimal.TryParse(value, out var p) && p >= 0 && p <= 100 ? (true, null) : (false, "Must be between 0 and 100"),
                "JSON" => IsValidJson(value) ? (true, null) : (false, "Must be valid JSON"),
                _ => (true, null)
            };
        }

        // ----------------------------------------------------------------
        // CACHE
        // ----------------------------------------------------------------

        public async Task ReloadCacheAsync()
        {
            _lastCacheRefresh = DateTime.MinValue;
            await EnsureCacheLoadedAsync();
        }

        // ----------------------------------------------------------------
        // SEED DEFAULTS
        // ----------------------------------------------------------------

        public async Task SeedDefaultSettingsAsync()
        {
            var defaults = new Dictionary<string, (string Value, string Category, string DataType, string Description)>
            {
                // General
                ["platform_name"] = ("ScholarRescue", "General", "String", "Platform display name"),
                ["platform_email"] = ("support@scholarrescue.com", "General", "String", "Primary platform email"),
                ["support_email"] = ("help@scholarrescue.com", "General", "String", "Customer support email"),
                ["default_currency"] = ("USD", "General", "String", "Default platform currency"),
                ["timezone"] = ("UTC", "General", "String", "Default timezone"),

                // Financial
                ["commission_percentage"] = ("10", "Financial", "Percentage", "Platform commission rate"),
                ["minimum_withdrawal"] = ("50", "Financial", "Decimal", "Minimum payout amount"),
                ["maximum_withdrawal"] = ("10000", "Financial", "Decimal", "Maximum payout amount"),
                ["refund_window_days"] = ("7", "Financial", "Integer", "Days to request refund"),
                ["late_fee_percentage"] = ("5", "Financial", "Percentage", "Late delivery fee"),

                // Marketplace
                ["max_applications_per_order"] = ("10", "Marketplace", "Integer", "Max writers who can apply"),
                ["order_expiration_days"] = ("14", "Marketplace", "Integer", "Days before open order expires"),

                // Writer
                ["max_active_orders"] = ("5", "Writer", "Integer", "Max concurrent orders per writer"),
                ["revision_window_hours"] = ("48", "Writer", "Integer", "Hours to complete revisions"),
                ["application_limit"] = ("20", "Writer", "Integer", "Max applications per day"),

                // Client
                ["max_open_orders"] = ("10", "Client", "Integer", "Max open orders per client"),
                ["max_attachments"] = ("5", "Client", "Integer", "Max files per upload"),
                ["revision_limits"] = ("3", "Client", "Integer", "Max free revisions per order"),

                // Escrow
                ["escrow_release_delay_hours"] = ("24", "Escrow", "Integer", "Hours before auto-release"),
                ["auto_approval_days"] = ("7", "Escrow", "Integer", "Days before auto-approval"),

                // Risk
                ["phone_risk_score"] = ("25", "Risk", "Integer", "Risk score for phone detection"),
                ["email_risk_score"] = ("25", "Risk", "Integer", "Risk score for email detection"),
                ["social_media_risk_score"] = ("20", "Risk", "Integer", "Risk score for social media"),
                ["payment_request_risk_score"] = ("40", "Risk", "Integer", "Risk score for payment request"),
                ["warning_threshold"] = ("25", "Risk", "Integer", "Score threshold for warnings"),
                ["suspension_threshold"] = ("75", "Risk", "Integer", "Score threshold for suspension"),

                // Moderation
                ["max_file_size_mb"] = ("25", "Moderation", "Integer", "Max upload file size in MB"),
                ["quarantine_threshold"] = ("50", "Moderation", "Integer", "Score threshold for quarantine"),

                // Communication
                ["messaging_enabled"] = ("true", "Communication", "Boolean", "Enable messaging system"),
                ["max_message_length"] = ("5000", "Communication", "Integer", "Max characters per message"),
                ["max_daily_messages"] = ("100", "Communication", "Integer", "Max messages per day"),

                // Security
                ["password_min_length"] = ("12", "Security", "Integer", "Minimum password length"),
                ["failed_login_limit"] = ("5", "Security", "Integer", "Login attempts before lockout"),
                ["lockout_duration_minutes"] = ("30", "Security", "Integer", "Account lockout duration"),
                ["session_timeout_minutes"] = ("60", "Security", "Integer", "Session idle timeout")
            };

            foreach (var (key, (value, category, dataType, description)) in defaults)
            {
                if (!await _context.PlatformSettings.AnyAsync(s => s.Key == key))
                {
                    _context.PlatformSettings.Add(new PlatformSetting
                    {
                        Key = key,
                        Value = value,
                        Category = category,
                        DataType = dataType,
                        Description = description,
                        IsEditable = true,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Seed feature flags
            var features = new (string Name, string Desc)[]
            {
                ("marketplace", "Enable order marketplace"),
                ("escrow", "Enable escrow system"),
                ("messaging", "Enable messaging"),
                ("notifications", "Enable notifications"),
                ("ai_risk_detection", "Enable AI risk detection"),
                ("moderation_engine", "Enable file moderation"),
                ("email_automation", "Enable email automation"),
                ("payment_gateways", "Enable payment gateways"),
            };

            foreach (var (name, desc) in features)
            {
                if (!await _context.FeatureFlags.AnyAsync(f => f.FeatureName == name))
                {
                    _context.FeatureFlags.Add(new FeatureFlag
                    {
                        FeatureName = name,
                        Enabled = true,
                        Description = desc,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Default settings and feature flags seeded.");
        }

        // ----------------------------------------------------------------
        // HELPERS
        // ----------------------------------------------------------------

        private async Task EnsureCacheLoadedAsync()
        {
            if (DateTime.UtcNow - _lastCacheRefresh < CacheDuration && _settingsCache.Count > 0)
                return;

            var settings = await _context.PlatformSettings.ToListAsync();
            _settingsCache = settings.ToDictionary(s => s.Key, s => s.Value);
            _lastCacheRefresh = DateTime.UtcNow;
        }

        private async Task EnsureFeatureCacheLoadedAsync()
        {
            if (DateTime.UtcNow - _lastCacheRefresh < CacheDuration && _featureCache.Count > 0)
                return;

            var flags = await _context.FeatureFlags.ToListAsync();
            _featureCache = flags.ToDictionary(f => f.FeatureName, f => f.Enabled);
            _lastCacheRefresh = DateTime.UtcNow;
        }

        private static string InferDataType(string value) => value switch
        {
            _ when bool.TryParse(value, out _) => "Boolean",
            _ when int.TryParse(value, out _) => "Integer",
            _ when decimal.TryParse(value, out _) => "Decimal",
            _ when IsValidJson(value) => "JSON",
            _ => "String"
        };

        private static bool IsValidJson(string value)
        {
            try { JsonDocument.Parse(value); return true; }
            catch { return false; }
        }
    }
}