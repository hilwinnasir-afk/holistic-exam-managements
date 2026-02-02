using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for batch year values (now supports string format like "Year IV Sem II")
    /// </summary>
    public class BatchYearAttribute : ValidationAttribute
    {
        public BatchYearAttribute() : base("Please enter a valid batch year (e.g., 'Year IV Sem II').")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            var batchYear = value.ToString()?.Trim();
            
            if (string.IsNullOrWhiteSpace(batchYear))
                return false;

            // Allow any non-empty string for batch year
            // This gives flexibility for different batch year formats
            return batchYear.Length >= 3 && batchYear.Length <= 50;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be a valid batch year format (e.g., 'Year IV Sem II').";
        }
    }
}