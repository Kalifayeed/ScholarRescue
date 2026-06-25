using ScholarRescue.Models;

namespace ScholarRescue.Services
{
    /// <summary>
    /// Centralized configuration service for platform settings and feature flags.
    /// Settings are cached for fast access and reloadable without redeployment.
    /// </summary>
    public interface IConfigurationService
    {
        // --- Settings ---
        /// <summary>Gets a setting value by key.</summary>
        Task<string?> GetSettingAsync(string key);

        /// <summary>Gets a typed setting value by key.</summary>
        Task<T> GetSettingAsync<T>(string key, T defaultValue);

        /// <summary>Sets a setting value.</summary>
        Task SetSettingAsync(string key, string value, string? updatedById = null);

        /// <summary>Gets all settings in a category.</summary>
        Task<List<PlatformSetting>> GetSettingsByCategoryAsync(string category);

        /// <summary>Gets all settings grouped by category.</summary>
        Task<Dictionary<string, List<PlatformSetting>>> GetAllSettingsAsync();

        // --- Feature Flags ---
        /// <summary>Checks if a feature is enabled.</summary>
        Task<bool> IsFeatureEnabledAsync(string featureName);

        /// <summary>Sets a feature flag.</summary>
        Task SetFeatureFlagAsync(string featureName, bool enabled, string? updatedById = null);

        /// <summary>Gets all feature flags.</summary>
        Task<List<FeatureFlag>> GetAllFeatureFlagsAsync();

        // --- Import / Export ---
        /// <summary>Exports all settings as JSON.</summary>
        Task<string> ExportSettingsAsync();

        /// <summary>Imports settings from JSON.</summary>
        Task<int> ImportSettingsAsync(string json, string? updatedById = null);

        // --- Validation ---
        /// <summary>Validates a setting value against its rules.</summary>
        (bool IsValid, string? Error) ValidateSetting(string key, string value, string dataType);

        // --- Cache ---
        /// <summary>Reloads the settings cache.</summary>
        Task ReloadCacheAsync();

        // --- Seed Defaults ---
        /// <summary>Seeds default settings if they don't exist.</summary>
        Task SeedDefaultSettingsAsync();
    }
}