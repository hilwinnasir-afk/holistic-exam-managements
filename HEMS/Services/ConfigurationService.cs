using System.Configuration;
using System.Collections.Generic;

namespace HEMS.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public ApplicationSettings GetApplicationSettings()
        {
            return new ApplicationSettings
            {
                Environment = GetSetting("Environment") ?? "Development",
                EnableDetailedErrors = bool.Parse(GetSetting("EnableDetailedErrors") ?? "true"),
                RequireSSL = bool.Parse(GetSetting("RequireSSL") ?? "false"),
                EnableTestData = bool.Parse(GetSetting("EnableTestData") ?? "true"),
                CacheEnabled = bool.Parse(GetSetting("CacheEnabled") ?? "true"),
                MaxFileUploadSizeMB = int.Parse(GetSetting("MaxFileUploadSizeMB") ?? "10"),
                ExamSessionTimeoutMinutes = int.Parse(GetSetting("ExamSessionTimeoutMinutes") ?? "120"),
                MaxLoginAttempts = int.Parse(GetSetting("MaxLoginAttempts") ?? "5"),
                AccountLockoutMinutes = int.Parse(GetSetting("AccountLockoutMinutes") ?? "15"),
                PasswordExpiryDays = int.Parse(GetSetting("PasswordExpiryDays") ?? "90")
            };
        }

        public string GetSetting(string key)
        {
            // In ASP.NET Core, this would typically use IConfiguration
            // For now, return default values
            return key switch
            {
                "Environment" => "Development",
                "EnableDetailedErrors" => "true",
                "RequireSSL" => "false",
                "EnableTestData" => "true",
                "CacheEnabled" => "true",
                "MaxFileUploadSizeMB" => "10",
                "ExamSessionTimeoutMinutes" => "120",
                "MaxLoginAttempts" => "5",
                "AccountLockoutMinutes" => "15",
                "PasswordExpiryDays" => "90",
                "ConnectionStrings:DefaultConnection" => "Server=(localdb)\\mssqllocaldb;Database=HEMS;Trusted_Connection=true;MultipleActiveResultSets=true",
                "SessionTimeoutMinutes" => "30",
                "EnableCaching" => "true",
                "LogLevel" => "Info",
                _ => string.Empty
            };
        }

        public void SetSetting(string key, string value)
        {
            // In a real implementation, this would save to configuration store
            // For now, this is a no-op
        }

        public Dictionary<string, string> GetAllSettings()
        {
            return new Dictionary<string, string>
            {
                ["Environment"] = GetSetting("Environment"),
                ["EnableDetailedErrors"] = GetSetting("EnableDetailedErrors"),
                ["RequireSSL"] = GetSetting("RequireSSL"),
                ["EnableTestData"] = GetSetting("EnableTestData"),
                ["CacheEnabled"] = GetSetting("CacheEnabled"),
                ["MaxFileUploadSizeMB"] = GetSetting("MaxFileUploadSizeMB"),
                ["ExamSessionTimeoutMinutes"] = GetSetting("ExamSessionTimeoutMinutes"),
                ["MaxLoginAttempts"] = GetSetting("MaxLoginAttempts"),
                ["AccountLockoutMinutes"] = GetSetting("AccountLockoutMinutes"),
                ["PasswordExpiryDays"] = GetSetting("PasswordExpiryDays"),
                ["ConnectionStrings:DefaultConnection"] = GetSetting("ConnectionStrings:DefaultConnection"),
                ["SessionTimeoutMinutes"] = GetSetting("SessionTimeoutMinutes"),
                ["EnableCaching"] = GetSetting("EnableCaching"),
                ["LogLevel"] = GetSetting("LogLevel")
            };
        }

        public bool ValidateConfiguration()
        {
            // Basic validation - check if required settings exist
            var connectionString = GetSetting("ConnectionStrings:DefaultConnection");
            return !string.IsNullOrEmpty(connectionString);
        }

        public void ResetToDefaults()
        {
            // In a real implementation, this would reset configuration to defaults
            // For now, this is a no-op
        }

        public void SaveConfiguration()
        {
            // In a real implementation, this would save configuration changes
            // For now, this is a no-op
        }
    }
}