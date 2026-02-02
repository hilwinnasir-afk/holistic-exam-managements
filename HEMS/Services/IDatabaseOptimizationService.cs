using System.Collections.Generic;

namespace HEMS.Services
{
    public interface IDatabaseOptimizationService
    {
        void OptimizeDatabase();
        void RebuildIndexes();
        void UpdateStatistics();
        void CleanupOldData();
        List<OptimizationResult> AnalyzePerformance();
        void ApplyOptimizationRecommendations();
        
        // Add missing methods
        DatabasePerformanceMetrics GetPerformanceMetrics();
        bool AreOptimizationsApplied();
        void ApplyDatabaseOptimizations();
        void UpdateDatabaseStatistics();
        void CreateAuthenticationIndexes();
        void CreateExamIndexes();
        void CreateGradingIndexes();
    }

    public class OptimizationResult
    {
        public string Category { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public string Severity { get; set; }
        public double ImpactScore { get; set; }
    }
    
    public class DatabasePerformanceMetrics
    {
        public double QueryExecutionTime { get; set; }
        public long DatabaseSize { get; set; }
        public int IndexCount { get; set; }
        public double CacheHitRatio { get; set; }
        public int ActiveConnections { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        
        // Add missing properties for views
        public string ErrorMessage { get; set; } = "";
        public int TotalIndexes { get; set; }
        public int UsedIndexes { get; set; }
        public double IndexUsagePercentage { get; set; }
        public Dictionary<string, object> TableStatistics { get; set; } = new Dictionary<string, object>();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}