using Microsoft.EntityFrameworkCore;
using HEMS.Models;
using System.Linq;

namespace HEMS.Models
{
    public class HEMSContext : DbContext
    {
        public HEMSContext(DbContextOptions<HEMSContext> options) : base(options)
        {
        }

        public HEMSContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HEMS;Trusted_Connection=true;MultipleActiveResultSets=true");
            }
        }

        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<Exam> Exams { get; set; }
        public virtual DbSet<Question> Questions { get; set; }
        public virtual DbSet<Choice> Choices { get; set; }
        public virtual DbSet<StudentExam> StudentExams { get; set; }
        public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }
        public virtual DbSet<ExamSession> ExamSessions { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<LoginSession> LoginSessions { get; set; }
        public virtual DbSet<LoginAttempt> LoginAttempts { get; set; }
        public virtual DbSet<PasswordHistory> PasswordHistory { get; set; }
        public virtual DbSet<FailedLoginAttempt> FailedLoginAttempts { get; set; }
        public virtual DbSet<SuccessfulLoginAttempt> SuccessfulLoginAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.IdNumber)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.UniversityEmail)
                .IsUnique();

            modelBuilder.Entity<StudentExam>()
                .HasIndex(se => new { se.StudentId, se.ExamId })
                .IsUnique();

            modelBuilder.Entity<StudentAnswer>()
                .HasIndex(sa => new { sa.StudentExamId, sa.QuestionId })
                .IsUnique();

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User-Student relationship (one-to-many)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Choice>()
                .HasOne(c => c.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Student)
                .WithMany(s => s.StudentExams)
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentExam>()
                .HasOne(se => se.Exam)
                .WithMany(e => e.StudentExams)
                .HasForeignKey(se => se.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.StudentExam)
                .WithMany(se => se.StudentAnswers)
                .HasForeignKey(sa => sa.StudentExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Choice)
                .WithMany(c => c.StudentAnswers)
                .HasForeignKey(sa => sa.ChoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamSession>()
                .HasOne(es => es.Exam)
                .WithMany(e => e.ExamSessions)
                .HasForeignKey(es => es.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AuditLog relationships
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.Student)
                .WithMany()
                .HasForeignKey(al => al.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.Exam)
                .WithMany()
                .HasForeignKey(al => al.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.StudentExam)
                .WithMany()
                .HasForeignKey(al => al.StudentExamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure LoginSession relationships
            modelBuilder.Entity<LoginSession>()
                .HasOne(ls => ls.User)
                .WithMany(u => u.LoginSessions)
                .HasForeignKey(ls => ls.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoginSession>()
                .HasOne(ls => ls.ExamSession)
                .WithMany()
                .HasForeignKey(ls => ls.ExamSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure User-CurrentExamSession relationship
            modelBuilder.Entity<User>()
                .HasOne(u => u.CurrentExamSession)
                .WithMany()
                .HasForeignKey(u => u.CurrentExamSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure LoginAttempt relationships
            modelBuilder.Entity<LoginAttempt>()
                .HasOne(la => la.User)
                .WithMany()
                .HasForeignKey(la => la.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure PasswordHistory relationships
            modelBuilder.Entity<PasswordHistory>()
                .HasOne(ph => ph.User)
                .WithMany()
                .HasForeignKey(ph => ph.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FailedLoginAttempt relationships
            modelBuilder.Entity<FailedLoginAttempt>()
                .HasOne(fla => fla.User)
                .WithMany()
                .HasForeignKey(fla => fla.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure SuccessfulLoginAttempt relationships
            modelBuilder.Entity<SuccessfulLoginAttempt>()
                .HasOne(sla => sla.User)
                .WithMany()
                .HasForeignKey(sla => sla.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for StudentExam properties
            modelBuilder.Entity<StudentExam>()
                .Property(se => se.Score)
                .HasPrecision(18, 2); // 18 digits total, 2 decimal places

            modelBuilder.Entity<StudentExam>()
                .Property(se => se.Percentage)
                .HasPrecision(5, 2); // 5 digits total, 2 decimal places (e.g., 100.00)

            base.OnModelCreating(modelBuilder);
        }

        #region Optimized Query Methods

        /// <summary>
        /// Optimized query for user authentication by email
        /// Uses IX_Users_Username_LoginPhase index
        /// </summary>
        public IQueryable<User> GetUsersByEmailOptimized(string email)
        {
            return Users
                .Where(u => u.Username == email)
                .Select(u => new User
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    PasswordHash = u.PasswordHash,
                    RoleId = u.RoleId,
                    LoginPhaseCompleted = u.LoginPhaseCompleted,
                    MustChangePassword = u.MustChangePassword,
                    IsLocked = u.IsLocked,
                    LockoutEndTime = u.LockoutEndTime,
                    FailedLoginAttempts = u.FailedLoginAttempts
                });
        }

        /// <summary>
        /// Optimized query for student lookup by ID number
        /// Uses IX_Students_IdNumber_UserId index
        /// </summary>
        public IQueryable<Student> GetStudentsByIdNumberOptimized(string idNumber)
        {
            return Students
                .Where(s => s.IdNumber == idNumber)
                .Select(s => new Student
                {
                    StudentId = s.StudentId,
                    UserId = s.UserId,
                    IdNumber = s.IdNumber,
                    UniversityEmail = s.UniversityEmail,
                    BatchYear = s.BatchYear,
                    User = s.User
                });
        }

        /// <summary>
        /// Optimized query for available exams
        /// Uses IX_Exams_AcademicYear_Published index
        /// </summary>
        public IQueryable<Exam> GetAvailableExamsOptimized(int academicYear)
        {
            return Exams
                .Where(e => e.AcademicYear == academicYear && e.IsPublished)
                .Select(e => new Exam
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    AcademicYear = e.AcademicYear,
                    DurationMinutes = e.DurationMinutes,
                    IsPublished = e.IsPublished,
                    CreatedDate = e.CreatedDate
                });
        }

        /// <summary>
        /// Optimized query for exam questions with ordering
        /// Uses IX_Questions_Exam_Order index
        /// </summary>
        public IQueryable<Question> GetExamQuestionsOptimized(int examId)
        {
            return Questions
                .Where(q => q.ExamId == examId)
                .OrderBy(q => q.QuestionOrder)
                .Select(q => new Question
                {
                    QuestionId = q.QuestionId,
                    ExamId = q.ExamId,
                    QuestionText = q.QuestionText,
                    QuestionOrder = q.QuestionOrder,
                    CreatedDate = q.CreatedDate
                });
        }

        /// <summary>
        /// Optimized query for question choices
        /// Uses IX_Choices_Question_Order index
        /// </summary>
        public IQueryable<Choice> GetQuestionChoicesOptimized(int questionId)
        {
            return Choices
                .Where(c => c.QuestionId == questionId)
                .OrderBy(c => c.ChoiceOrder)
                .Select(c => new Choice
                {
                    ChoiceId = c.ChoiceId,
                    QuestionId = c.QuestionId,
                    ChoiceText = c.ChoiceText,
                    IsCorrect = c.IsCorrect,
                    ChoiceOrder = c.ChoiceOrder
                });
        }

        /// <summary>
        /// Optimized query for student answers
        /// Uses IX_StudentAnswers_StudentExam_Question index
        /// </summary>
        public IQueryable<StudentAnswer> GetStudentAnswersOptimized(int studentExamId)
        {
            return StudentAnswers
                .Where(sa => sa.StudentExamId == studentExamId)
                .Select(sa => new StudentAnswer
                {
                    StudentAnswerId = sa.StudentAnswerId,
                    StudentExamId = sa.StudentExamId,
                    QuestionId = sa.QuestionId,
                    ChoiceId = sa.ChoiceId,
                    IsFlagged = sa.IsFlagged,
                    LastModified = sa.LastModified
                });
        }

        /// <summary>
        /// Optimized query for student exam status
        /// Uses IX_StudentExams_Student_Exam_Submitted index
        /// </summary>
        public IQueryable<StudentExam> GetStudentExamStatusOptimized(int studentId, int examId)
        {
            return StudentExams
                .Where(se => se.StudentId == studentId && se.ExamId == examId)
                .Select(se => new StudentExam
                {
                    StudentExamId = se.StudentExamId,
                    StudentId = se.StudentId,
                    ExamId = se.ExamId,
                    StartDateTime = se.StartDateTime,
                    SubmitDateTime = se.SubmitDateTime,
                    Score = se.Score,
                    Percentage = se.Percentage,
                    IsSubmitted = se.IsSubmitted,
                    GradedDateTime = se.GradedDateTime
                });
        }

        /// <summary>
        /// Optimized query for correct answers during grading
        /// Uses IX_Choices_Question_Correct index
        /// </summary>
        public IQueryable<Choice> GetCorrectAnswersOptimized(int questionId)
        {
            return Choices
                .Where(c => c.QuestionId == questionId && c.IsCorrect)
                .Select(c => new Choice
                {
                    ChoiceId = c.ChoiceId,
                    QuestionId = c.QuestionId,
                    ChoiceText = c.ChoiceText,
                    IsCorrect = c.IsCorrect
                });
        }

        /// <summary>
        /// Optimized query for active login sessions
        /// Uses IX_LoginSessions_User_Phase_Active index
        /// </summary>
        public IQueryable<LoginSession> GetActiveLoginSessionsOptimized(int userId, int loginPhase)
        {
            return LoginSessions
                .Where(ls => ls.UserId == userId && ls.LoginPhase == loginPhase && ls.IsActive)
                .Select(ls => new LoginSession
                {
                    LoginSessionId = ls.LoginSessionId,
                    UserId = ls.UserId,
                    LoginPhase = ls.LoginPhase,
                    SessionToken = ls.SessionToken,
                    LoginTime = ls.LoginTime,
                    IsActive = ls.IsActive,
                    ExamSessionId = ls.ExamSessionId
                });
        }

        #endregion
    }
}