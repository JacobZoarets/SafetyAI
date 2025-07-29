using System;
using System.Runtime.Caching;
using System.Threading.Tasks;
using SafetyAI.Services.Infrastructure;

namespace SafetyAI.Services.Caching
{
    public class CacheService : ICacheService, IDisposable
    {
        private readonly MemoryCache _cache;
        private bool _disposed = false;

        public CacheService()
        {
            _cache = MemoryCache.Default;
        }

        public T Get<T>(string key) where T : class
        {
            try
            {
                return _cache.Get(key) as T;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache get error for key {key}: {ex.Message}", "CacheService");
                return null;
            }
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            return await Task.FromResult(Get<T>(key));
        }

        public void Set<T>(string key, T value, TimeSpan expiration) where T : class
        {
            try
            {
                if (value == null)
                    return;

                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(expiration),
                    Priority = CacheItemPriority.Default
                };

                _cache.Set(key, value, policy);
                Logger.LogInfo($"Cache set for key {key} with expiration {expiration}", "CacheService");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache set error for key {key}: {ex.Message}", "CacheService");
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            await Task.Run(() => Set(key, value, expiration));
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
                Logger.LogInfo($"Cache removed for key {key}", "CacheService");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache remove error for key {key}: {ex.Message}", "CacheService");
            }
        }

        public async Task RemoveAsync(string key)
        {
            await Task.Run(() => Remove(key));
        }

        public bool Exists(string key)
        {
            try
            {
                return _cache.Contains(key);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache exists check error for key {key}: {ex.Message}", "CacheService");
                return false;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await Task.FromResult(Exists(key));
        }

        public void Clear()
        {
            try
            {
                // MemoryCache doesn't have a direct clear method, so we'll dispose and recreate
                // This is not ideal for production - consider using a different cache implementation
                foreach (var item in _cache)
                {
                    _cache.Remove(item.Key);
                }
                Logger.LogInfo("Cache cleared", "CacheService");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache clear error: {ex.Message}", "CacheService");
            }
        }

        public async Task ClearAsync()
        {
            await Task.Run(() => Clear());
        }

        public T GetOrSet<T>(string key, Func<T> factory, TimeSpan expiration) where T : class
        {
            try
            {
                var cached = Get<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                var value = factory();
                if (value != null)
                {
                    Set(key, value, expiration);
                }

                return value;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache GetOrSet error for key {key}: {ex.Message}", "CacheService");
                return factory(); // Fallback to factory method
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class
        {
            try
            {
                var cached = await GetAsync<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                var value = await factory();
                if (value != null)
                {
                    await SetAsync(key, value, expiration);
                }

                return value;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Cache GetOrSetAsync error for key {key}: {ex.Message}", "CacheService");
                return await factory(); // Fallback to factory method
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cache?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public interface ICacheService
    {
        T Get<T>(string key) where T : class;
        Task<T> GetAsync<T>(string key) where T : class;
        void Set<T>(string key, T value, TimeSpan expiration) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
        void Remove(string key);
        Task RemoveAsync(string key);
        bool Exists(string key);
        Task<bool> ExistsAsync(string key);
        void Clear();
        Task ClearAsync();
        T GetOrSet<T>(string key, Func<T> factory, TimeSpan expiration) where T : class;
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration) where T : class;
    }
}