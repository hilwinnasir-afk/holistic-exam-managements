using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    [Table("LoginAttempts")]
    public class LoginAttempt
    {
        [Key]
        public int LoginAttemptId { get; set; }

        [StringLength(100)]
        public string Username { get; set; } // Email for Phase 1, ID Number for Phase 2

        [Required]
        public int LoginPhase { get; set; } // 1 for Phase 1, 2 for Phase 2

        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        public bool IsSuccessful { get; set; }

        [Required]
        public AuthenticationErrorType ErrorType { get; set; }

        [StringLength(500)]
        public string ErrorMessage { get; set; }

        public DateTime AttemptTime { get; set; } = DateTime.Now;

        public int? UserId { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}