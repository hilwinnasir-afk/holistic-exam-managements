using HEMS.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace HEMS.Tests
{
    /// <summary>
    /// Simple standalone test for basic cache functionality
    /// </summary>
    [TestClass]
    public class SimpleCacheTest
    {
        private ICacheService _cacheService;

        [TestInitialize]
        public void Setup()
        {
            _cacheService = new CacheService();
            _cacheService.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cacheService?.Clear();
            _cacheService?.Dispose();
        }

        [TestMethod]
        public void BasicCacheOperations_Should_Work()
        {
            // Test basic string caching
            _cacheService.Set("test_key", "test_value");
            var result = _cacheService.Get<string>("test_key");
            Assert.AreEqual("test_value", result);

            // Test cache exists
            Assert.IsTrue(_cacheService.Exists("test_key"));

            // Test cache removal
            _cacheService.Remove("test_key");
            Assert.IsFalse(_cacheService.Exists("test_key"));
            Assert.IsNull(_cacheService.Get<string>("test_key"));
        }

        [TestMethod]
        public void CacheExpiration_Should_Work()
        {
            // Set with short expiration
            _cacheService.Set("expiring_key", "expiring_value", TimeSpan.FromMilliseconds(50));
            
            // Should exist immediately
            Assert.IsTrue(_cacheService.Exists("expiring_key"));
            Assert.AreEqual("expiring_value", _cacheService.Get<string>("expiring_key"));

            // Wait for expiration
            System.Threading.Thread.Sleep(100);

            // Should be expired
            Assert.IsFalse(_cacheService.Exists("expiring_key"));
            Assert.IsNull(_cacheService.Get<string>("expiring_key"));
        }

        [TestMethod]
        public void CacheStatistics_Should_Track_Operations()
        {
            _cacheService.ResetStatistics();
            
            // Set a value
            _cacheService.Set("stats_key", "stats_value");
            
            // Generate hits and misses
            _cacheService.Get<string>("stats_key"); // Hit
            _cacheService.Get<string>("nonexistent"); // Miss
            _cacheService.Get<string>("stats_key"); // Hit

            var stats = _cacheService.GetCacheStatistics();
            
            Assert.AreEqual(2L, stats["CacheHits"]);
            Assert.AreEqual(1L, stats["CacheMisses"]);
            Assert.AreEqual(3L, stats["TotalRequests"]);
            
            var hitRate = _cacheService.GetCacheHitRate();
            Assert.AreEqual(66.67, Math.Round(hitRate, 2));
        }

        [TestMethod]
        public void PatternRemoval_Should_Work()
        {
            // Set multiple keys
            _cacheService.Set("user:1", "user1");
            _cacheService.Set("user:2", "user2");
            _cacheService.Set("exam:1", "exam1");
            _cacheService.Set("config:setting", "value");

            // Remove by pattern
            _cacheService.RemoveByPattern("user:*");

            // Check results
            Assert.IsNull(_cacheService.Get<string>("user:1"));
            Assert.IsNull(_cacheService.Get<string>("user:2"));
            Assert.IsNotNull(_cacheService.Get<string>("exam:1"));
            Assert.IsNotNull(_cacheService.Get<string>("config:setting"));
        }
    }
}