using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    /// <summary>
    /// Model for tracking failed login attempts for security monitoring and audit purposes
    /// </summary>
    [Table("FailedLoginAttempts")]
    public class FailedLoginAttempt
    {
        [Key]
        public int FailedLoginAttemptId { get; set; }

        /// <summary>
        /// Email or ID number used in the failed login attempt
        /// </summary>
        [StringLength(100)]
        public string Identifier { get; set; }

        /// <summary>
        /// Login phase (1 for Phase 1, 2 for Phase 2)
        /// </summary>
        public int LoginPhase { get; set; }

        /// <summary>
        /// Type of authentication error that occurred
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ErrorType { get; set; }

        /// <summary>
        /// IP address of the client making the failed attempt
        /// </summary>
        [StringLength(45)] // IPv6 addresses can be up to 45 characters
        public string IpAddress { get; set; }

        /// <summary>
        /// User agent string of the client
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Timestamp when the failed attempt occurred
        /// </summary>
        public DateTime AttemptTime { get; set; } = DateTime.Now;

        /// <summary>
        /// User ID if the user was identified (optional)
        /// </summary>
        public int? UserId { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}