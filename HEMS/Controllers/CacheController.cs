using HEMS.Attributes;
using HEMS.Services;
using HEMS.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace HEMS.Controllers
{
    /// <summary>
    /// Controller for cache management and monitoring operations
    /// Provides administrative interface for cache operations
    /// </summary>
    [RoleAuthorize(Roles = "Coordinator")]
    public class CacheController : Controller
    {
        private readonly ICacheManagementService _cacheManagementService;
        private readonly ICacheService _cacheService;

        public CacheController(ICacheManagementService cacheManagementService, ICacheService cacheService)
        {
            _cacheManagementService = cacheManagementService ?? throw new ArgumentNullException(nameof(cacheManagementService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        #region Cache Monitoring Views

        /// <summary>
        /// Main cache dashboard showing health and performance metrics
        /// </summary>
        public ActionResult Index()
        {
            try
            {
                var model = new CacheDashboardViewModel
                {
                    HealthStatus = _cacheManagementService.GetCacheHealth(),
                    PerformanceMetrics = _cacheManagementService.GetPerformanceMetrics(),
                    UsageStatistics = _cacheManagementService.GetUsageStatistics(),
                    Configuration = _cacheManagementService.GetCacheConfiguration()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading cache dashboard: {ex.Message}";
                return View(new CacheDashboardViewModel());
            }
        }

        /// <summary>
        /// Detailed performance metrics view
        /// </summary>
        public ActionResult Performance()
        {
            try
            {
                var model = new CachePerformanceViewModel
                {
                    Metrics = _cacheManagementService.GetPerformanceMetrics(),
                    HealthStatus = _cacheManagementService.GetCacheHealth(),
                    CategoryStats = _cacheManagementService.GetUsageStatistics()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading performance metrics: {ex.Message}";
                return View(new CachePerformanceViewModel());
            }
        }

        /// <summary>
        /// Cache configuration management view
        /// </summary>
        public ActionResult Configuration()
        {
            try
            {
                var model = new CacheConfigurationViewModel
                {
                    Configuration = _cacheManagementService.GetCacheConfiguration(),
                    HealthStatus = _cacheManagementService.GetCacheHealth()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading cache configuration: {ex.Message}";
                return View(new CacheConfigurationViewModel());
            }
        }

        #endregion

        #region Cache Management Actions

        /// <summary>
        /// Clears all cached data
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearAll()
        {
            try
            {
                _cacheManagementService.ClearCacheCategory("all");
                TempData["SuccessMessage"] = "All cache data has been cleared successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error clearing cache: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Clears cache for a specific category
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCategory(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    TempData["ErrorMessage"] = "Category is required.";
                    return RedirectToAction("Index");
                }

                _cacheManagementService.ClearCacheCategory(category);
                TempData["SuccessMessage"] = $"Cache category '{category}' has been cleared successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error clearing cache category: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Preloads cache with frequently accessed data
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PreloadCache(string[] categories = null)
        {
            try
            {
                _cacheManagementService.PreloadCache(categories);
                var categoryList = categories != null ? string.Join(", ", categories) : "all categories";
                TempData["SuccessMessage"] = $"Cache preloaded successfully for {categoryList}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error preloading cache: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Warms up cache for a specific exam
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult WarmupExam(int examId)
        {
            try
            {
                _cacheManagementService.WarmupExamCache(examId);
                TempData["SuccessMessage"] = $"Cache warmed up successfully for exam {examId}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error warming up exam cache: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Optimizes cache by cleaning up expired entries
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OptimizeCache()
        {
            try
            {
                _cacheManagementService.OptimizeCache();
                var cleanedCount = _cacheManagementService.CleanupExpiredEntries();
                TempData["SuccessMessage"] = $"Cache optimized successfully. Cleaned up {cleanedCount} expired entries.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error optimizing cache: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Resets cache statistics
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetStatistics()
        {
            try
            {
                _cacheService.ResetStatistics();
                TempData["SuccessMessage"] = "Cache statistics have been reset successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error resetting statistics: {ex.Message}";
            }

            return RedirectToAction("Performance");
        }

        /// <summary>
        /// Updates cache configuration
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateConfiguration(CacheConfigurationUpdateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Invalid configuration data.";
                    return RedirectToAction("Configuration");
                }

                var settings = model.ToConfigurationDictionary();
                _cacheManagementService.UpdateCacheConfiguration(settings);
                TempData["SuccessMessage"] = "Cache configuration updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating configuration: {ex.Message}";
            }

            return RedirectToAction("Configuration");
        }

        #endregion

        #region AJAX Actions for Real-time Updates

        /// <summary>
        /// Gets current cache statistics as JSON for AJAX updates
        /// </summary>
        public JsonResult GetCacheStatistics()
        {
            try
            {
                var stats = new
                {
                    Health = _cacheManagementService.GetCacheHealth(),
                    Performance = _cacheManagementService.GetPerformanceMetrics(),
                    Timestamp = DateTime.Now
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets cache hit rate for dashboard widgets
        /// </summary>
        public JsonResult GetHitRate()
        {
            try
            {
                var hitRate = _cacheService.GetCacheHitRate();
                return Json(new { hitRate = hitRate, timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets cache health status for monitoring
        /// </summary>
        public JsonResult GetHealthStatus()
        {
            try
            {
                var health = _cacheManagementService.GetCacheHealth();
                return Json(health);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cache services are managed by DI container
                // No manual disposal needed
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region View Models

    public class CacheDashboardViewModel
    {
        public CacheHealthStatus HealthStatus { get; set; } = new CacheHealthStatus();
        public CachePerformanceMetrics PerformanceMetrics { get; set; } = new CachePerformanceMetrics();
        public Dictionary<string, CacheCategoryStats> UsageStatistics { get; set; } = new Dictionary<string, CacheCategoryStats>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    public class CachePerformanceViewModel
    {
        public CachePerformanceMetrics Metrics { get; set; } = new CachePerformanceMetrics();
        public CacheHealthStatus HealthStatus { get; set; } = new CacheHealthStatus();
        public Dictionary<string, CacheCategoryStats> CategoryStats { get; set; } = new Dictionary<string, CacheCategoryStats>();
    }

    public class CacheConfigurationViewModel
    {
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        public CacheHealthStatus HealthStatus { get; set; } = new CacheHealthStatus();
    }

    public class CacheConfigurationUpdateModel
    {
        public int UserExpirationMinutes { get; set; } = 15;
        public int ExamExpirationHours { get; set; } = 2;
        public int QuestionExpirationHours { get; set; } = 4;
        public int StudentAnswerExpirationMinutes { get; set; } = 5;
        public int ConfigurationExpirationHours { get; set; } = 24;
        public int ExamSessionExpirationMinutes { get; set; } = 10;
        
        public bool UsersEnabled { get; set; } = true;
        public bool ExamsEnabled { get; set; } = true;
        public bool QuestionsEnabled { get; set; } = true;
        public bool StudentAnswersEnabled { get; set; } = true;
        public bool ConfigurationEnabled { get; set; } = true;
        public bool ExamSessionsEnabled { get; set; } = true;

        public Dictionary<string, object> ToConfigurationDictionary()
        {
            var expirations = new Dictionary<string, TimeSpan>
            {
                ["users"] = TimeSpan.FromMinutes(UserExpirationMinutes),
                ["exams"] = TimeSpan.FromHours(ExamExpirationHours),
                ["questions"] = TimeSpan.FromHours(QuestionExpirationHours),
                ["student_answers"] = TimeSpan.FromMinutes(StudentAnswerExpirationMinutes),
                ["configuration"] = TimeSpan.FromHours(ConfigurationExpirationHours),
                ["exam_sessions"] = TimeSpan.FromMinutes(ExamSessionExpirationMinutes)
            };

            var enabled = new Dictionary<string, bool>
            {
                ["users"] = UsersEnabled,
                ["exams"] = ExamsEnabled,
                ["questions"] = QuestionsEnabled,
                ["student_answers"] = StudentAnswersEnabled,
                ["configuration"] = ConfigurationEnabled,
                ["exam_sessions"] = ExamSessionsEnabled
            };

            return new Dictionary<string, object>
            {
                ["CategoryExpirations"] = expirations,
                ["CategoryEnabled"] = enabled
            };
        }
    }

    #endregion
}