using System;

namespace HEMS.Models
{
    /// <summary>
    /// Enumeration of all possible authentication error types
    /// </summary>
    public enum AuthenticationErrorType
    {
        None = 0,
        
        // Phase 1 Authentication Errors
        InvalidEmailFormat,
        UserNotFound,
        IncorrectPassword,
        AccountLocked,
        AccountDisabled,
        MultipleFailedAttempts,
        Phase1AlreadyCompleted,
        EmailDomainNotAllowed,
        StudentRecordNotFound,
        
        // Phase 2 Authentication Errors
        InvalidIdNumberFormat,
        ExamSessionNotFound,
        ExpiredExamSession,
        IncorrectExamPassword,
        Phase1NotCompleted,
        ConcurrentLoginAttempt,
        ExamSessionPasswordExpired,
        
        // Password Change Errors
        WeakPassword,
        PasswordReuse,
        ConfirmationMismatch,
        PasswordTooShort,
        PasswordTooLong,
        PasswordMissingRequiredCharacters,
        
        // Session Management Errors
        SessionTimeout,
        InvalidSessionToken,
        SessionHijackingDetected,
        MaxConcurrentSessionsExceeded,
        
        // System Errors
        DatabaseConnectionError,
        SystemError,
        ValidationError,
        UnauthorizedAccess,
        ServiceUnavailable
    }

    /// <summary>
    /// Result class for authentication operations with detailed error information
    /// </summary>
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public bool IsAuthenticated => IsSuccess;
        public AuthenticationErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string DetailedErrorMessage { get; set; }
        public User User { get; set; }
        public string SessionToken { get; set; }
        public int? ExamSessionId { get; set; }
        public DateTime? LockoutEndTime { get; set; }
        public int FailedAttemptCount { get; set; }
        public bool RequiresPasswordChange { get; set; }

        public static AuthenticationResult Success(User user, string sessionToken = null, int? examSessionId = null)
        {
            return new AuthenticationResult
            {
                IsSuccess = true,
                ErrorType = AuthenticationErrorType.None,
                User = user,
                SessionToken = sessionToken,
                ExamSessionId = examSessionId
            };
        }

        public static AuthenticationResult Failure(AuthenticationErrorType errorType, string errorMessage, string detailedMessage = null)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                DetailedErrorMessage = detailedMessage ?? errorMessage
            };
        }

        public static AuthenticationResult SystemError(string errorMessage = "A system error occurred. Please try again.")
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorType = AuthenticationErrorType.SystemError,
                ErrorMessage = errorMessage,
                DetailedErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Result class for password change operations
    /// </summary>
    public class PasswordChangeResult
    {
        public bool IsSuccess { get; set; }
        public AuthenticationErrorType ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string DetailedErrorMessage { get; set; }

        public static PasswordChangeResult Success()
        {
            return new PasswordChangeResult
            {
                IsSuccess = true,
                ErrorType = AuthenticationErrorType.None
            };
        }

        public static PasswordChangeResult Failure(AuthenticationErrorType errorType, string errorMessage, string detailedMessage = null)
        {
            return new PasswordChangeResult
            {
                IsSuccess = false,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                DetailedErrorMessage = detailedMessage ?? errorMessage
            };
        }
    }
}