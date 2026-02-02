using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HEMS.Services
{
    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache;
        private long _hitCount = 0;
        private long _missCount = 0;

        public CacheService()
        {
            _cache = new ConcurrentDictionary<string, CacheItem>();
        }

        public T Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt == null || item.ExpiresAt > DateTime.UtcNow)
                {
                    System.Threading.Interlocked.Increment(ref _hitCount);
                    return (T)item.Value;
                }
                else
                {
                    _cache.TryRemove(key, out _);
                }
            }
            System.Threading.Interlocked.Increment(ref _missCount);
            return default(T);
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;
            var item = new CacheItem
            {
                Value = value,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };
            _cache.AddOrUpdate(key, item, (k, v) => item);
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public bool Exists(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (item.ExpiresAt == null || item.ExpiresAt > DateTime.UtcNow)
                {
                    return true;
                }
                else
                {
                    _cache.TryRemove(key, out _);
                }
            }
            return false;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public List<string> GetKeys()
        {
            return _cache.Keys.ToList();
        }

        public void SetExpiration(string key, TimeSpan expiration)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                item.ExpiresAt = DateTime.UtcNow.Add(expiration);
            }
        }

        public TimeSpan? GetTimeToLive(string key)
        {
            if (_cache.TryGetValue(key, out var item) && item.ExpiresAt.HasValue)
            {
                var ttl = item.ExpiresAt.Value - DateTime.UtcNow;
                return ttl > TimeSpan.Zero ? ttl : TimeSpan.Zero;
            }
            return null;
        }

        public void ResetStatistics()
        {
            System.Threading.Interlocked.Exchange(ref _hitCount, 0);
            System.Threading.Interlocked.Exchange(ref _missCount, 0);
        }

        public double GetCacheHitRate()
        {
            var totalRequests = _hitCount + _missCount;
            if (totalRequests == 0) return 0.0;
            return (double)_hitCount / totalRequests;
        }

        private class CacheItem
        {
            public object? Value { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}