namespace HEMS.Models
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string[] Errors { get; set; }
        public string[] Warnings { get; set; }

        public ValidationResult()
        {
            Errors = new string[0];
            Warnings = new string[0];
        }
    }
}