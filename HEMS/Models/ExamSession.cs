using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    [Table("ExamSessions")]
    public class ExamSession
    {
        [Key]
        public int ExamSessionId { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        [StringLength(100)]
        public string SessionPassword { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        // Navigation Properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }
    }
}