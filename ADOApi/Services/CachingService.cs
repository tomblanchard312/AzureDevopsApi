using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace ADOApi.Services
{
    public interface ICachingService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        void Remove(string key);
        void Clear();
    }

    public class CachingService : ICachingService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingService> _logger;

        public CachingService(IMemoryCache cache, ILogger<CachingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug($"Cache hit for key: {key}");
                return cachedValue;
            }

            _logger.LogDebug($"Cache miss for key: {key}");
            var value = await factory();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(expiration ?? TimeSpan.FromMinutes(5));

            _cache.Set(key, value, cacheOptions);
            return value;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug($"Removed cache entry for key: {key}");
        }

        public void Clear()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
                _logger.LogInformation("Cache cleared");
            }
        }
    }
} 