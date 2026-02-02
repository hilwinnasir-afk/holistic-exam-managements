using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    [Table("StudentAnswers")]
    public class StudentAnswer
    {
        [Key]
        public int StudentAnswerId { get; set; }

        [Required]
        public int StudentExamId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? ChoiceId { get; set; }

        public bool IsFlagged { get; set; } = false;

        public DateTime LastModified { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("StudentExamId")]
        public virtual StudentExam StudentExam { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }

        [ForeignKey("ChoiceId")]
        public virtual Choice Choice { get; set; }
    }
}