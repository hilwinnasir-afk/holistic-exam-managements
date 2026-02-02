using System.Collections.Generic;

namespace HEMS.Models
{
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        
        // Add missing properties for views
        public bool HasErrors => Errors?.Count > 0;
        public bool HasWarnings => Warnings?.Count > 0;
        public int ErrorCount => Errors?.Count ?? 0;
        public int WarningCount => Warnings?.Count ?? 0;

        public ConfigurationValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            IsValid = true;
        }
    }
}