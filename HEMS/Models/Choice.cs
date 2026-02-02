using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEMS.Attributes;

namespace HEMS.Models
{
    [Table("Choices")]
    public class Choice
    {
        [Key]
        public int ChoiceId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Choice text is required")]
        [StringLength(500, ErrorMessage = "Choice text cannot exceed 500 characters")]
        [ChoiceText(1, 500)]
        public string ChoiceText { get; set; }

        [Required]
        public bool IsCorrect { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Choice order must be a positive number")]
        public int ChoiceOrder { get; set; }

        // Navigation Properties
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }

        public virtual ICollection<StudentAnswer> StudentAnswers { get; set; }

        public Choice()
        {
            StudentAnswers = new HashSet<StudentAnswer>();
        }
    }
}