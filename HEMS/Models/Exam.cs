using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEMS.Attributes;

namespace HEMS.Models
{
    [Table("Exams")]
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Exam title is required")]
        [StringLength(200, ErrorMessage = "Exam title cannot exceed 200 characters")]
        [ExamTitle(5, 200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        [AcademicYear]
        public int AcademicYear { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [ExamDuration(30, 480)]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Exam start date and time is required")]
        public DateTime ExamStartDateTime { get; set; }

        [Required(ErrorMessage = "Exam end date and time is required")]
        public DateTime ExamEndDateTime { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<StudentExam> StudentExams { get; set; }
        public virtual ICollection<ExamSession> ExamSessions { get; set; }

        public Exam()
        {
            Questions = new HashSet<Question>();
            StudentExams = new HashSet<StudentExam>();
            ExamSessions = new HashSet<ExamSession>();
        }
    }
}