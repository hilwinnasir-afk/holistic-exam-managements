using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    [Table("LoginSessions")]
    public class LoginSession
    {
        [Key]
        public int LoginSessionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string SessionToken { get; set; }

        [Required]
        public int LoginPhase { get; set; } // 1 for Phase 1, 2 for Phase 2

        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public int? ExamSessionId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime? LogoutTime { get; set; }

        public DateTime? EndTime { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("ExamSessionId")]
        public virtual ExamSession ExamSession { get; set; }
    }
}