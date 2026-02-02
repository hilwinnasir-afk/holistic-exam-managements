using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Models
{
    /// <summary>
    /// Represents an audit log entry for tracking critical exam events
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Gets or sets the unique identifier for the audit log entry
        /// </summary>
        [Key]
        public int AuditLogId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the event occurred
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the type of event that occurred
        /// </summary>
        [Required]
        [StringLength(50)]
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the description of the event
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who performed the action (if applicable)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the student involved (if applicable)
        /// </summary>
        public int? StudentId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the exam involved (if applicable)
        /// </summary>
        public int? ExamId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the student exam session (if applicable)
        /// </summary>
        public int? StudentExamId { get; set; }

        /// <summary>
        /// Gets or sets the IP address from which the action was performed
        /// </summary>
        [StringLength(45)] // IPv6 max length
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets additional data related to the event (JSON format)
        /// </summary>
        public string AdditionalData { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the event
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Severity { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Student? Student { get; set; }
        public virtual Exam? Exam { get; set; }
        public virtual StudentExam? StudentExam { get; set; }
    }

    /// <summary>
    /// Enumeration of audit event types
    /// </summary>
    public static class AuditEventTypes
    {
        public const string ExamStarted = "EXAM_STARTED";
        public const string ExamSubmitted = "EXAM_SUBMITTED";
        public const string ExamAutoSubmitted = "EXAM_AUTO_SUBMITTED";
        public const string AnswerSaved = "ANSWER_SAVED";
        public const string QuestionFlagged = "QUESTION_FLAGGED";
        public const string QuestionUnflagged = "QUESTION_UNFLAGGED";
        public const string ExamAccessed = "EXAM_ACCESSED";
        public const string ExamCreated = "EXAM_CREATED";
        public const string ExamPublished = "EXAM_PUBLISHED";
        public const string ExamUnpublished = "EXAM_UNPUBLISHED";
        public const string StudentImported = "STUDENT_IMPORTED";
        public const string UserLogin = "USER_LOGIN";
        public const string UserLogout = "USER_LOGOUT";
        public const string PasswordChanged = "PASSWORD_CHANGED";
        public const string ExamSessionCreated = "EXAM_SESSION_CREATED";
        public const string UnauthorizedAccess = "UNAUTHORIZED_ACCESS";
        public const string TimerExpired = "TIMER_EXPIRED";
        public const string ExamGraded = "EXAM_GRADED";
        public const string ResultsViewed = "RESULTS_VIEWED";
        public const string DataSynced = "DATA_SYNCED";
        public const string AnswerSynced = "ANSWER_SYNCED";
    }

    /// <summary>
    /// Enumeration of audit severity levels
    /// </summary>
    public static class AuditSeverity
    {
        public const string Info = "INFO";
        public const string Warning = "WARNING";
        public const string Error = "ERROR";
        public const string Critical = "CRITICAL";
    }
}