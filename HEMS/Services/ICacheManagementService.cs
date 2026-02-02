using System.Collections.Generic;
using HEMS.Models;

namespace HEMS.Services
{
    public interface ICacheManagementService
    {
        void ClearAllCaches();
        void ClearCache(string cacheType);
        void ClearCacheCategory(string category);
        List<CacheStatistics> GetCacheStatistics();
        void WarmupCache();
        void PreloadCache();
        void PreloadCache(string[] categories);
        void WarmupExamCache();
        void WarmupExamCache(int examId);
        void OptimizeCache();
        int CleanupExpiredEntries();
        void UpdateCacheConfiguration(CacheConfiguration config);
        void UpdateCacheConfiguration(Dictionary<string, object> settings);
        CacheHealthStatus GetCacheHealth();
        CachePerformanceMetrics GetPerformanceMetrics();
        Dictionary<string, CacheCategoryStats> GetUsageStatistics();
        Dictionary<string, object> GetCacheConfiguration();
        void ConfigureCache(string cacheType, CacheConfiguration config);
    }

    public class CacheStatistics
    {
        public string CacheType { get; set; }
        public int ItemCount { get; set; }
        public long MemoryUsage { get; set; }
        public double HitRate { get; set; }
        public double MissRate { get; set; }
    }

    public class CacheConfiguration
    {
        public int MaxItems { get; set; }
        public int DefaultExpirationMinutes { get; set; }
        public bool EnableCompression { get; set; }
    }
}