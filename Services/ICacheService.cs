namespace ScholarRescue.Services
{
    /// <summary>
    /// Distributed cache abstraction for high-performance data retrieval.
    /// Reduces database load by caching frequently accessed data in memory/Redis.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>Get a cached value by key.</summary>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>Set a cached value with expiration.</summary>
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;

        /// <summary>Get or create a cached value using a factory function.</summary>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class;

        /// <summary>Remove a cached value.</summary>
        Task RemoveAsync(string key);

        /// <summary>Build a namespaced cache key.</summary>
        string BuildKey(params string[] segments);
    }
}