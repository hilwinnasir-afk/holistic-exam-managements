namespace HEMS.Models
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ExamValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>A validation result indicating success</returns>
        public static ExamValidationResult Success()
        {
            return new ExamValidationResult { IsValid = true };
        }

        /// <summary>
        /// Creates a failed validation result with an error message
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        /// <returns>A validation result indicating failure</returns>
        public static ExamValidationResult Failure(string errorMessage)
        {
            return new ExamValidationResult { IsValid = false, ErrorMessage = errorMessage };
        }
    }
}