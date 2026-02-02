using System.Collections.Generic;
using HEMS.Models;

namespace HEMS.Services
{
    public interface IConfigurationService
    {
        string GetSetting(string key);
        void SetSetting(string key, string value);
        Dictionary<string, string> GetAllSettings();
        bool ValidateConfiguration();
        void ResetToDefaults();
        void SaveConfiguration();
        
        // Add missing methods
        ApplicationSettings GetApplicationSettings();
    }
    
    // Add ApplicationSettings class
    public class ApplicationSettings
    {
        public string Environment { get; set; } = "Development";
        public bool EnableDetailedErrors { get; set; } = true;
        public bool RequireSSL { get; set; } = false;
        public bool EnableTestData { get; set; } = true;
        public bool CacheEnabled { get; set; } = true;
        public int MaxFileUploadSizeMB { get; set; } = 10;
        public int ExamSessionTimeoutMinutes { get; set; } = 120;
        public int MaxLoginAttempts { get; set; } = 5;
        public int AccountLockoutMinutes { get; set; } = 15;
        public int PasswordExpiryDays { get; set; } = 90;
        
        // Add missing properties for views
        public string LogLevel { get; set; } = "Info";
        public int SessionTimeoutMinutes { get; set; } = 30;
        public string DatabaseConnectionString { get; set; } = "Server=(localdb)\\mssqllocaldb;Database=HEMS;Trusted_Connection=true;MultipleActiveResultSets=true";
    }
}