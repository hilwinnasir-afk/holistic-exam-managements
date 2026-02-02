using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    [Table("StudentExams")]
    public class StudentExam
    {
        [Key]
        public int StudentExamId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        public DateTime? SubmitDateTime { get; set; }

        public decimal? Score { get; set; }

        public decimal? Percentage { get; set; }

        public DateTime? GradedDateTime { get; set; }

        public bool IsSubmitted { get; set; } = false;

        // Navigation Properties
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; }

        public StudentExam()
        {
            StudentAnswers = new HashSet<StudentAnswer>();
        }
    }
}