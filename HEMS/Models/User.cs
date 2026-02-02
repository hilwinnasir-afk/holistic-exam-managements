using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        public int RoleId { get; set; }

        public bool LoginPhaseCompleted { get; set; } = false;

        public bool MustChangePassword { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastPhase2Login { get; set; }

        public int? CurrentExamSessionId { get; set; }

        public bool IsLocked { get; set; } = false;

        public DateTime? LockoutEndTime { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastFailedLoginAttempt { get; set; }

        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public virtual Student Student { get; set; }

        public virtual ICollection<LoginSession> LoginSessions { get; set; }

        [ForeignKey("CurrentExamSessionId")]
        public virtual ExamSession CurrentExamSession { get; set; }
    }
}