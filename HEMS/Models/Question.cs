using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEMS.Attributes;

namespace HEMS.Models
{
    [Table("Questions")]
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [Column(TypeName = "ntext")]
        [QuestionText(10, 2000)]
        public string QuestionText { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Question order must be a positive number")]
        public int QuestionOrder { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        public virtual ICollection<Choice> Choices { get; set; }
        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; }

        public Question()
        {
            Choices = new HashSet<Choice>();
            StudentAnswers = new HashSet<StudentAnswer>();
        }
    }
}