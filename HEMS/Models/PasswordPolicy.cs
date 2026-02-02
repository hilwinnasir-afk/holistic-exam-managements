using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HEMS.Models
{
    /// <summary>
    /// Password policy configuration and validation
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinimumLength = 8;
        public const int MaximumLength = 128;
        public const bool RequireUppercase = true;
        public const bool RequireLowercase = true;
        public const bool RequireDigit = true;
        public const bool RequireSpecialCharacter = true;
        public const int MaxFailedAttempts = 5;
        public const int LockoutDurationMinutes = 30;
        public const int PasswordHistoryCount = 5; // Number of previous passwords to remember

        /// <summary>
        /// Validates password against policy requirements
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>Validation result with specific error details</returns>
        public static PasswordValidationResult ValidatePassword(string password)
        {
            var result = new PasswordValidationResult { IsValid = true };
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                result.IsValid = false;
                result.ErrorType = AuthenticationErrorType.ValidationError;
                result.ErrorMessage = "Password is required.";
                return result;
            }

            if (password.Length < MinimumLength)
            {
                errors.Add($"Password must be at least {MinimumLength} characters long.");
                result.ErrorType = AuthenticationErrorType.PasswordTooShort;
            }

            if (password.Length > MaximumLength)
            {
                errors.Add($"Password must not exceed {MaximumLength} characters.");
                result.ErrorType = AuthenticationErrorType.PasswordTooLong;
            }

            if (RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("Password must contain at least one uppercase letter.");
                result.ErrorType = AuthenticationErrorType.PasswordMissingRequiredCharacters;
            }

            if (RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("Password must contain at least one lowercase letter.");
                result.ErrorType = AuthenticationErrorType.PasswordMissingRequiredCharacters;
            }

            if (RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
            {
                errors.Add("Password must contain at least one digit.");
                result.ErrorType = AuthenticationErrorType.PasswordMissingRequiredCharacters;
            }

            if (RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                errors.Add("Password must contain at least one special character.");
                result.ErrorType = AuthenticationErrorType.PasswordMissingRequiredCharacters;
            }

            // Check for common weak patterns
            if (IsWeakPassword(password))
            {
                errors.Add("Password is too weak. Avoid common patterns, dictionary words, or sequential characters.");
                result.ErrorType = AuthenticationErrorType.WeakPassword;
            }

            if (errors.Count > 0)
            {
                result.IsValid = false;
                result.ErrorMessage = string.Join(" ", errors);
            }

            return result;
        }

        /// <summary>
        /// Checks if password contains weak patterns
        /// </summary>
        /// <param name="password">Password to check</param>
        /// <returns>True if password is considered weak</returns>
        private static bool IsWeakPassword(string password)
        {
            var lowerPassword = password.ToLower();

            // Common weak patterns
            var weakPatterns = new[]
            {
                "password", "123456", "qwerty", "abc123", "admin", "user",
                "test", "guest", "welcome", "login", "pass", "root"
            };

            foreach (var pattern in weakPatterns)
            {
                if (lowerPassword.Contains(pattern))
                    return true;
            }

            // Sequential characters
            if (Regex.IsMatch(password, @"(012|123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)"))
                return true;

            // Repeated characters
            if (Regex.IsMatch(password, @"(.)\1{2,}"))
                return true;

            return false;
        }
    }

    /// <summary>
    /// Result of password validation
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public AuthenticationErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
}