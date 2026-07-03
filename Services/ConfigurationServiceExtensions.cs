namespace ScholarRescue.Services
{
    public static class ConfigurationServiceExtensions
    {
        public static async Task<decimal> GetCommissionRateAsync(this IConfigurationService configurationService)
        {
            var percentage = await configurationService.GetSettingAsync("commission_percentage", 10m);
            var rate = percentage / 100m;
            if (rate < 0m) return 0m;
            if (rate > 1m) return 1m;
            return rate;
        }
    }
}
