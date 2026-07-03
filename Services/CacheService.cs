using Microsoft.Extensions.Caching.Memory;

namespace ScholarRescue.Services
{
    /// <summary>
    /// In-memory cache service for high-performance data retrieval.
    /// Reduces database load by caching frequently accessed data.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                _logger.LogTrace("Cache HIT for key: {Key}", key);
                return cached;
            }
            _logger.LogTrace("Cache MISS for key: {Key}", key);
            return null;
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration)
                .SetSlidingExpiration(TimeSpan.FromMinutes(1))
                .SetSize(1);

            _cache.Set(key, value, options);
            _logger.LogTrace("Cache SET for key: {Key} (expires in {Expiry})", key, expiration);
            return Task.CompletedTask;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached != null) return cached;

            var value = await factory();
            if (value != null)
                await SetAsync(key, value, expiration);

            return value!;
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _logger.LogTrace("Cache REMOVED for key: {Key}", key);
            return Task.CompletedTask;
        }

        public string BuildKey(params string[] segments)
        {
            return string.Join(":", segments);
        }
    }
}