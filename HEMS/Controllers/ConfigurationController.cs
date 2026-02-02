using HEMS.Services;
using HEMS.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HEMS.Controllers
{
    /// <summary>
    /// Controller for configuration management and validation
    /// Provides administrative interface for system configuration
    /// </summary>
    [Authorize(Roles = "Coordinator")]
    public class ConfigurationController : Controller
    {
        private readonly IConfigurationService _configurationService;
        private readonly ConfigurationValidator _configurationValidator;

        public ConfigurationController(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _configurationValidator = new ConfigurationValidator(_configurationService);
        }

        /// <summary>
        /// Display configuration overview
        /// </summary>
        public ActionResult Index()
        {
            try
            {
                var settings = _configurationService.GetApplicationSettings();
                return View(settings);
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = $"Failed to load configuration: {ex.Message}";
                return View(new ApplicationSettings());
            }
        }

        /// <summary>
        /// Validate current configuration
        /// </summary>
        public ActionResult Validate()
        {
            try
            {
                var validationResult = _configurationValidator.ValidateAll();
                return View(validationResult);
            }
            catch (System.Exception ex)
            {
                var errorResult = new ConfigurationValidationResult();
                errorResult.Errors.Add($"Configuration validation failed: {ex.Message}");
                errorResult.IsValid = false;
                return View(errorResult);
            }
        }

        /// <summary>
        /// Get configuration as JSON for AJAX requests
        /// </summary>
        public JsonResult GetConfiguration()
        {
            try
            {
                var settings = _configurationService.GetApplicationSettings();
                return Json(new { success = true, data = settings });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Validate configuration via AJAX
        /// </summary>
        public JsonResult ValidateConfiguration()
        {
            try
            {
                var validationResult = _configurationValidator.ValidateAll();
                return Json(new 
                { 
                    success = true, 
                    isValid = validationResult.IsValid,
                    errors = validationResult.Errors,
                    warnings = validationResult.Warnings,
                    errorCount = validationResult.ErrorCount,
                    warningCount = validationResult.WarningCount
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}