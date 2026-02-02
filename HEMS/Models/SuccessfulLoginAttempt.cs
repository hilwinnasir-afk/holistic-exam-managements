using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    /// <summary>
    /// Model for tracking successful login attempts for audit purposes
    /// </summary>
    [Table("SuccessfulLoginAttempts")]
    public class SuccessfulLoginAttempt
    {
        [Key]
        public int SuccessfulLoginAttemptId { get; set; }

        /// <summary>
        /// Email or ID number used in the successful login
        /// </summary>
        [StringLength(100)]
        public string Identifier { get; set; }

        /// <summary>
        /// Login phase (1 for Phase 1, 2 for Phase 2)
        /// </summary>
        public int LoginPhase { get; set; }

        /// <summary>
        /// IP address of the client making the successful login
        /// </summary>
        [StringLength(45)] // IPv6 addresses can be up to 45 characters
        public string IpAddress { get; set; }

        /// <summary>
        /// User agent string of the client
        /// </summary>
        [StringLength(500)]
        public string UserAgent { get; set; }

        /// <summary>
        /// Timestamp when the successful login occurred
        /// </summary>
        public DateTime LoginTime { get; set; } = DateTime.Now;

        /// <summary>
        /// User ID of the user who logged in successfully
        /// </summary>
        [Required]
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}