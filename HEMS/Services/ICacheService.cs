using System;
using System.Collections.Generic;

namespace HEMS.Services
{
    public interface ICacheService
    {
        T Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        bool Exists(string key);
        void Clear();
        List<string> GetKeys();
        void SetExpiration(string key, TimeSpan expiration);
        TimeSpan? GetTimeToLive(string key);
        void ResetStatistics();
        double GetCacheHitRate();
    }
}