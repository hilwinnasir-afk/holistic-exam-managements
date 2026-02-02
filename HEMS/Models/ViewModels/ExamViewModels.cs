using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HEMS.Attributes;

namespace HEMS.Models.ViewModels
{
    /// <summary>
    /// View model for creating exams
    /// </summary>
    public class ExamCreateViewModel
    {
        [Required(ErrorMessage = "Exam title is required")]
        [Display(Name = "Exam Title")]
        [ExamTitle(5, 200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        [Display(Name = "Academic Year")]
        [AcademicYear]
        public int AcademicYear { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Display(Name = "Duration (Minutes)")]
        [ExamDuration(30, 480)]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Exam start date and time is required")]
        [Display(Name = "Exam Start Date & Time")]
        public DateTime ExamStartDateTime { get; set; }

        [Required(ErrorMessage = "Exam end date and time is required")]
        [Display(Name = "Exam End Date & Time")]
        public DateTime ExamEndDateTime { get; set; }

        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }

        public ExamCreateViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
            AcademicYear = DateTime.Now.Year;
            DurationMinutes = 120; // Default 2 hours
            ExamStartDateTime = DateTime.Now.AddDays(1); // Default to tomorrow
            ExamEndDateTime = DateTime.Now.AddDays(1).AddHours(2); // Default to tomorrow + 2 hours
        }
    }

    /// <summary>
    /// View model for managing exams
    /// </summary>
    public class ExamManageViewModel
    {
        public Exam Exam { get; set; }
        public List<Question> Questions { get; set; }
        public int TotalQuestions { get; set; }
        public bool CanAddQuestions { get; set; }
        public bool CanPublish { get; set; }
        public bool CanEdit { get; set; }
        public List<string> ValidationMessages { get; set; }

        public ExamManageViewModel()
        {
            Questions = new List<Question>();
            ValidationMessages = new List<string>();
            CanAddQuestions = true;
            CanPublish = true;
            CanEdit = true;
        }
    }

    /// <summary>
    /// View model for creating questions
    /// </summary>
    public class QuestionCreateViewModel
    {
        [Required(ErrorMessage = "Exam ID is required")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [Display(Name = "Question Text")]
        [QuestionText(10, 2000)]
        public string QuestionText { get; set; }

        [Required(ErrorMessage = "Choice 1 is required")]
        [Display(Name = "Choice 1")]
        [ChoiceText(1, 500)]
        public string Choice1 { get; set; }

        [Required(ErrorMessage = "Choice 2 is required")]
        [Display(Name = "Choice 2")]
        [ChoiceText(1, 500)]
        public string Choice2 { get; set; }

        [Display(Name = "Choice 3")]
        [ChoiceText(1, 500)]
        public string Choice3 { get; set; }

        [Display(Name = "Choice 4")]
        [ChoiceText(1, 500)]
        public string Choice4 { get; set; }

        [Required(ErrorMessage = "Correct choice must be selected")]
        [Display(Name = "Correct Choice")]
        [Range(0, 3, ErrorMessage = "Correct choice must be between 0 and 3")]
        public int CorrectChoiceIndex { get; set; }

        [Display(Name = "Question Order")]
        [Range(1, int.MaxValue, ErrorMessage = "Question order must be a positive number")]
        public int? QuestionOrder { get; set; }

        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }

        public QuestionCreateViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
        }
    }

    /// <summary>
    /// View model for exam session management
    /// </summary>
    public class ExamSessionViewModel
    {
        [Required(ErrorMessage = "Exam ID is required")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Session password is required")]
        [Display(Name = "Session Password")]
        [SessionPassword(6, 50)]
        public string SessionPassword { get; set; }

        [Display(Name = "Expiry Hours")]
        [Range(1, 48, ErrorMessage = "Expiry hours must be between 1 and 48")]
        public int ExpiryHours { get; set; } = 24;

        [Display(Name = "Auto-activate")]
        public bool AutoActivate { get; set; } = true;

        public Exam Exam { get; set; }
        public List<ExamSession> ActiveSessions { get; set; }
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }

        public ExamSessionViewModel()
        {
            ActiveSessions = new List<ExamSession>();
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
        }
    }

    /// <summary>
    /// View model for exam publishing
    /// </summary>
    public class ExamPublishViewModel
    {
        public int ExamId { get; set; }
        public Exam Exam { get; set; }
        public int QuestionCount { get; set; }
        public int ChoiceCount { get; set; }
        public bool HasCorrectAnswers { get; set; }
        public bool CanPublish { get; set; }
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }
        public List<string> PublishingChecklist { get; set; }

        public ExamPublishViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
            PublishingChecklist = new List<string>();
        }
    }

    /// <summary>
    /// View model for exam validation results
    /// </summary>
    public class ExamValidationViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; }
        public DateTime ValidationDate { get; set; }

        public ExamValidationViewModel()
        {
            Issues = new List<ValidationIssue>();
            ValidationDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Represents a validation issue
    /// </summary>
    public class ValidationIssue
    {
        public string Category { get; set; }
        public string Message { get; set; }
        public string Severity { get; set; }
        public string Field { get; set; }
        public object Value { get; set; }
        public string SuggestedAction { get; set; }
    }

    /// <summary>
    /// View model for bulk question entry
    /// </summary>
    public class BulkQuestionEntryViewModel
    {
        public int ExamId { get; set; }
        
        [Required(ErrorMessage = "Questions text is required")]
        [Display(Name = "Questions (Bulk Entry)")]
        public string QuestionsText { get; set; }
        
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }
        public List<ParsedQuestion> ParsedQuestions { get; set; }
        
        public BulkQuestionEntryViewModel()
        {
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
            ParsedQuestions = new List<ParsedQuestion>();
        }
    }

    /// <summary>
    /// Represents a parsed question from bulk entry
    /// </summary>
    public class ParsedQuestion
    {
        public string QuestionText { get; set; }
        public List<string> Choices { get; set; }
        public int CorrectChoiceIndex { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsValid { get; set; }
        
        public ParsedQuestion()
        {
            Choices = new List<string>();
        }
    }
}