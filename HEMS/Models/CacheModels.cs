using System;
using System.Collections.Generic;

namespace HEMS.Models
{
    public class CacheHealthStatus
    {
        public bool IsHealthy { get; set; } = true;
        public string Status { get; set; } = "Healthy";
        public DateTime LastChecked { get; set; } = DateTime.Now;
        public DateTime LastHealthCheck { get; set; } = DateTime.Now;
        public double HitRate { get; set; }
        public long TotalMemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public double AverageResponseTime { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> HealthIssues { get; set; } = new List<string>();
    }

    public class CachePerformanceMetrics
    {
        public double HitRate { get; set; }
        public double MissRate { get; set; }
        public long TotalRequests { get; set; }
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double AverageResponseTime { get; set; }
        public double AverageHitTime { get; set; }
        public double AverageMissTime { get; set; }
        public long MemoryUsage { get; set; }
    }

    public class CacheCategoryStats
    {
        public string Category { get; set; }
        public int ItemCount { get; set; }
        public int EntryCount { get; set; }
        public long MemoryUsage { get; set; }
        public double HitRate { get; set; }
        public double MissRate { get; set; }
        public DateTime LastAccessed { get; set; }
        public TimeSpan AverageExpiration { get; set; }
        
        // Add missing properties
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public int TotalRequests { get; set; }
    }
}