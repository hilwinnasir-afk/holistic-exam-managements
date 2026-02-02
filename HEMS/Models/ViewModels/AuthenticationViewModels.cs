using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HEMS.Attributes;

namespace HEMS.Models.ViewModels
{
    /// <summary>
    /// View model for Phase 1 login
    /// </summary>
    public class Phase1LoginViewModel
    {
        [Required(ErrorMessage = "University email is required")]
        [Display(Name = "University Email")]
        [UniversityEmail(ErrorMessage = "Please enter a valid university email address from an approved educational institution")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }

        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }
        public int FailedAttemptCount { get; set; }
        public DateTime? LockoutEndTime { get; set; }

        public Phase1LoginViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
        }
    }

    /// <summary>
    /// View model for Phase 2 login
    /// </summary>
    public class Phase2LoginViewModel
    {
        [Required(ErrorMessage = "Student ID number is required")]
        [Display(Name = "Student ID Number")]
        [StudentId]
        public string IdNumber { get; set; } = "";

        [Required(ErrorMessage = "Session password is required")]
        [Display(Name = "Session Password")]
        [DataType(DataType.Password)]
        [SessionPassword(6, 50)]
        public string Password { get; set; } = "";

        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }
        public int FailedAttemptCount { get; set; }
        public DateTime? LockoutEndTime { get; set; }
        public bool ShowPhase1Link { get; set; }

        public Phase2LoginViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
        }
    }

    /// <summary>
    /// View model for password change
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "New password is required")]
        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match")]
        public string ConfirmPassword { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }
        public List<string> PasswordRequirements { get; set; }
        public bool ShowRequirements { get; set; } = true;

        public ChangePasswordViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
            PasswordRequirements = new List<string>();
        }
    }

    /// <summary>
    /// View model for authentication status
    /// </summary>
    public class AuthenticationStatusViewModel
    {
        public bool IsAuthenticated { get; set; }
        public bool Phase1Completed { get; set; }
        public bool Phase2Completed { get; set; }
        public bool MustChangePassword { get; set; }
        public string UserRole { get; set; }
        public string Username { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public int FailedAttemptCount { get; set; }
        public DateTime? LockoutEndTime { get; set; }
        public List<string> SecurityMessages { get; set; }

        public AuthenticationStatusViewModel()
        {
            SecurityMessages = new List<string>();
        }
    }

    /// <summary>
    /// View model for coordinator login
    /// </summary>
    public class CoordinatorLoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public string? ErrorMessage { get; set; }

        public CoordinatorLoginViewModel()
        {
        }
    }
    public class PasswordResetViewModel
    {
        [Required(ErrorMessage = "University email is required")]
        [Display(Name = "University Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [UniversityEmail(ErrorMessage = "Please enter a valid university email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Student ID is required")]
        [Display(Name = "Student ID Number")]
        [StudentId]
        public string IdNumber { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }
        public List<string> ValidationErrors { get; set; }

        public PasswordResetViewModel()
        {
            ValidationErrors = new List<string>();
        }
    }

    /// <summary>
    /// View model for account lockout information
    /// </summary>
    public class AccountLockoutViewModel
    {
        public string Username { get; set; }
        public DateTime LockoutEndTime { get; set; }
        public int FailedAttemptCount { get; set; }
        public int MaxFailedAttempts { get; set; }
        public TimeSpan RemainingLockoutTime { get; set; }
        public string LockoutReason { get; set; }
        public List<string> SecurityTips { get; set; }

        public AccountLockoutViewModel()
        {
            SecurityTips = new List<string>();
        }
    }

    /// <summary>
    /// View model for authentication audit log
    /// </summary>
    public class AuthenticationAuditViewModel
    {
        public string Username { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<AuthenticationAuditEntry> Entries { get; set; }
        public int TotalEntries { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public AuthenticationAuditViewModel()
        {
            Entries = new List<AuthenticationAuditEntry>();
            StartDate = DateTime.Now.AddDays(-30);
            EndDate = DateTime.Now;
            PageSize = 50;
            PageNumber = 1;
        }
    }

    /// <summary>
    /// Authentication audit entry
    /// </summary>
    public class AuthenticationAuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Username { get; set; }
        public string EventType { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string AdditionalData { get; set; }
    }
}