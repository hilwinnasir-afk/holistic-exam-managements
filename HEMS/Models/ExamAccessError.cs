using System;
using System.Collections.Generic;

namespace HEMS.Models
{
    /// <summary>
    /// Enumeration of exam access error types
    /// </summary>
    public enum ExamAccessErrorType
    {
        // Exam Availability Issues
        ExamNotFound,
        ExamNotPublished,
        ExamNotForCurrentYear,
        StudentNotEligible,
        ExamAlreadyCompleted,
        
        // Exam Session Issues
        InvalidSessionPassword,
        ExpiredSessionPassword,
        MultipleActiveAttempts,
        SessionTimeout,
        
        // Exam Taking Issues
        NetworkConnectivityIssue,
        InvalidQuestionNavigation,
        TimerExpired,
        SubmissionFailure,
        
        // System Issues
        DatabaseConnectivityError,
        ServiceUnavailable,
        InsufficientResources,
        MaintenanceMode,
        
        // Security Issues
        UnauthorizedAccess,
        DataIntegrityViolation,
        SuspiciousActivity,
        
        // General Issues
        UnknownError
    }

    /// <summary>
    /// Represents an exam access error with user-friendly messaging
    /// </summary>
    public class ExamAccessError
    {
        public ExamAccessErrorType ErrorType { get; set; }
        public string UserFriendlyMessage { get; set; }
        public string TechnicalMessage { get; set; }
        public string SuggestedAction { get; set; }
        public string ContactInfo { get; set; }
        public string RedirectUrl { get; set; }
        public bool ShowRetryOption { get; set; }
        public bool ShowContactSupport { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }

        public ExamAccessError()
        {
            Timestamp = DateTime.Now;
            AdditionalData = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates an exam access error with user-friendly messaging
        /// </summary>
        public static ExamAccessError Create(ExamAccessErrorType errorType, string technicalMessage = null, Dictionary<string, object> additionalData = null)
        {
            var error = new ExamAccessError
            {
                ErrorType = errorType,
                TechnicalMessage = technicalMessage,
                AdditionalData = additionalData ?? new Dictionary<string, object>()
            };

            SetUserFriendlyMessage(error);
            return error;
        }

        private static void SetUserFriendlyMessage(ExamAccessError error)
        {
            switch (error.ErrorType)
            {
                // Exam Availability Issues
                case ExamAccessErrorType.ExamNotFound:
                    error.UserFriendlyMessage = "The requested exam could not be found.";
                    error.SuggestedAction = "Please check with your coordinator to ensure the exam has been created and published.";
                    error.ContactInfo = "Contact your exam coordinator for assistance.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.ExamNotPublished:
                    error.UserFriendlyMessage = "This exam is not yet available for students.";
                    error.SuggestedAction = "The exam coordinator has not yet published this exam. Please wait for the coordinator to make it available.";
                    error.ContactInfo = "Contact your exam coordinator if you believe this exam should be available.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.ExamNotForCurrentYear:
                    error.UserFriendlyMessage = "This exam is not available for the current academic year.";
                    error.SuggestedAction = "Only exams for the current academic year are accessible. Please check if there's a current exam available.";
                    error.ContactInfo = "Contact your exam coordinator if you need access to this exam.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.StudentNotEligible:
                    error.UserFriendlyMessage = "You are not eligible to take this exam.";
                    error.SuggestedAction = "Please ensure you have completed Phase 1 identity verification and are registered for this exam.";
                    error.ContactInfo = "Contact your exam coordinator to verify your eligibility.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.ExamAlreadyCompleted:
                    error.UserFriendlyMessage = "You have already completed and submitted this exam.";
                    error.SuggestedAction = "Each exam can only be taken once. You can view your results if they are available.";
                    error.ContactInfo = "Contact your exam coordinator if you believe this is an error.";
                    error.ShowContactSupport = true;
                    break;

                // Exam Session Issues
                case ExamAccessErrorType.InvalidSessionPassword:
                    error.UserFriendlyMessage = "The exam session password is incorrect.";
                    error.SuggestedAction = "Please enter the password announced by your exam coordinator. Make sure you're entering it exactly as provided.";
                    error.ContactInfo = "Ask your exam coordinator for the correct session password.";
                    error.ShowRetryOption = true;
                    break;

                case ExamAccessErrorType.ExpiredSessionPassword:
                    error.UserFriendlyMessage = "The exam session has ended or the password has expired.";
                    error.SuggestedAction = "The exam session may have ended, or you may have taken too long to log in.";
                    error.ContactInfo = "Contact your exam coordinator immediately for assistance.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.MultipleActiveAttempts:
                    error.UserFriendlyMessage = "You already have an active exam session running.";
                    error.SuggestedAction = "Please complete your current exam session or contact your coordinator to reset your session.";
                    error.ContactInfo = "Contact your exam coordinator if you need to reset your session.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.SessionTimeout:
                    error.UserFriendlyMessage = "Your exam session has timed out due to inactivity.";
                    error.SuggestedAction = "Your session was automatically ended for security reasons. Any saved answers have been preserved.";
                    error.ContactInfo = "Contact your exam coordinator to resume your exam if possible.";
                    error.ShowContactSupport = true;
                    break;

                // Exam Taking Issues
                case ExamAccessErrorType.NetworkConnectivityIssue:
                    error.UserFriendlyMessage = "There appears to be a network connectivity issue.";
                    error.SuggestedAction = "Please check your internet connection and try again. Your answers are automatically saved when possible.";
                    error.ContactInfo = "If the problem persists, notify your exam coordinator immediately.";
                    error.ShowRetryOption = true;
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.InvalidQuestionNavigation:
                    error.UserFriendlyMessage = "Unable to navigate to the requested question.";
                    error.SuggestedAction = "Please try navigating to a different question or refresh the page.";
                    error.ContactInfo = "If you continue to have navigation issues, contact your exam coordinator.";
                    error.ShowRetryOption = true;
                    break;

                case ExamAccessErrorType.TimerExpired:
                    error.UserFriendlyMessage = "The exam time limit has been reached.";
                    error.SuggestedAction = "Your exam has been automatically submitted. All saved answers have been recorded.";
                    error.ContactInfo = "Contact your exam coordinator if you believe there was a timing error.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.SubmissionFailure:
                    error.UserFriendlyMessage = "There was a problem submitting your exam.";
                    error.SuggestedAction = "Please try submitting again. Your answers have been saved and will not be lost.";
                    error.ContactInfo = "If submission continues to fail, notify your exam coordinator immediately.";
                    error.ShowRetryOption = true;
                    error.ShowContactSupport = true;
                    break;

                // System Issues
                case ExamAccessErrorType.DatabaseConnectivityError:
                    error.UserFriendlyMessage = "The system is temporarily experiencing technical difficulties.";
                    error.SuggestedAction = "Please wait a moment and try again. The system should be available shortly.";
                    error.ContactInfo = "If the problem persists, contact your exam coordinator or technical support.";
                    error.ShowRetryOption = true;
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.ServiceUnavailable:
                    error.UserFriendlyMessage = "The exam system is currently unavailable.";
                    error.SuggestedAction = "The system may be undergoing maintenance or experiencing high load. Please try again in a few minutes.";
                    error.ContactInfo = "Contact your exam coordinator if the system remains unavailable.";
                    error.ShowRetryOption = true;
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.InsufficientResources:
                    error.UserFriendlyMessage = "The system is currently at capacity.";
                    error.SuggestedAction = "Too many students may be accessing the system simultaneously. Please wait a few minutes and try again.";
                    error.ContactInfo = "Contact your exam coordinator if you cannot access the system after multiple attempts.";
                    error.ShowRetryOption = true;
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.MaintenanceMode:
                    error.UserFriendlyMessage = "The exam system is currently under maintenance.";
                    error.SuggestedAction = "System maintenance is in progress. Please wait for the maintenance to complete.";
                    error.ContactInfo = "Contact your exam coordinator for information about when the system will be available.";
                    error.ShowContactSupport = true;
                    break;

                // Security Issues
                case ExamAccessErrorType.UnauthorizedAccess:
                    error.UserFriendlyMessage = "You are not authorized to access this exam or exam session.";
                    error.SuggestedAction = "Please ensure you are logged in with the correct account and have completed all required authentication steps.";
                    error.ContactInfo = "Contact your exam coordinator if you believe you should have access to this exam.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.DataIntegrityViolation:
                    error.UserFriendlyMessage = "A data integrity issue has been detected.";
                    error.SuggestedAction = "For security reasons, this action cannot be completed. Your exam session may need to be reset.";
                    error.ContactInfo = "Contact your exam coordinator immediately for assistance.";
                    error.ShowContactSupport = true;
                    break;

                case ExamAccessErrorType.SuspiciousActivity:
                    error.UserFriendlyMessage = "Unusual activity has been detected in your exam session.";
                    error.SuggestedAction = "For security reasons, your session may be temporarily restricted. Please follow exam guidelines.";
                    error.ContactInfo = "Contact your exam coordinator if you believe this is an error.";
                    error.ShowContactSupport = true;
                    break;

                // General Issues
                case ExamAccessErrorType.UnknownError:
                default:
                    error.UserFriendlyMessage = "An unexpected error has occurred.";
                    error.SuggestedAction = "Please try again. If the problem persists, contact support.";
                    error.ContactInfo = "Contact your exam coordinator or technical support for assistance.";
                    error.ShowRetryOption = true;
                    error.ShowContactSupport = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Enhanced exam validation result with detailed error information
    /// </summary>
    public class EnhancedExamValidationResult
    {
        public bool IsValid { get; set; }
        public ExamAccessError Error { get; set; }
        public string RedirectUrl { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; }

        public EnhancedExamValidationResult()
        {
            AdditionalData = new Dictionary<string, object>();
        }

        public static EnhancedExamValidationResult Success()
        {
            return new EnhancedExamValidationResult { IsValid = true };
        }

        public static EnhancedExamValidationResult Failure(ExamAccessErrorType errorType, string technicalMessage = null, string redirectUrl = null, Dictionary<string, object> additionalData = null)
        {
            return new EnhancedExamValidationResult
            {
                IsValid = false,
                Error = ExamAccessError.Create(errorType, technicalMessage, additionalData),
                RedirectUrl = redirectUrl,
                AdditionalData = additionalData ?? new Dictionary<string, object>()
            };
        }
    }
}