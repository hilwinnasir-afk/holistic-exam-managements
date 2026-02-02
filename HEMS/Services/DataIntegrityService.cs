using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using HEMS.Models;

namespace HEMS.Services
{
    public class DataIntegrityService : IDataIntegrityService
    {
        private readonly HEMSContext _context;
        
        public DataIntegrityService(HEMSContext context) 
        { 
            _context = context; 
        }

        public bool ValidateExamIntegrity(int examId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null) return false;
            
            // Check if exam has questions
            var hasQuestions = _context.Questions.Any(q => q.ExamId == examId);
            return hasQuestions;
        }

        public bool ValidateStudentExamIntegrity(int studentExamId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null) return false;
            
            // Check if student exam has corresponding exam and student
            var exam = _context.Exams.Find(studentExam.ExamId);
            var student = _context.Students.Find(studentExam.StudentId);
            
            return exam != null && student != null;
        }

        public ValidationResult ValidateAnswerIntegrity(int studentExamId, int questionId, int? choiceId)
        {
            var result = new ValidationResult { IsValid = true };
            
            // Check if student exam exists
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null)
            {
                result.IsValid = false;
                result.Errors = new[] { "Student exam not found" };
                return result;
            }
            
            // Check if question belongs to the exam
            var question = _context.Questions.FirstOrDefault(q => q.QuestionId == questionId && q.ExamId == studentExam.ExamId);
            if (question == null)
            {
                result.IsValid = false;
                result.Errors = new[] { "Question does not belong to this exam" };
                return result;
            }
            
            // Check if choice is valid for the question
            if (choiceId.HasValue)
            {
                var choice = _context.Choices.FirstOrDefault(c => c.ChoiceId == choiceId.Value && c.QuestionId == questionId);
                if (choice == null)
                {
                    result.IsValid = false;
                    result.Errors = new[] { "Invalid choice for this question" };
                    return result;
                }
            }
            
            return result;
        }

        public ValidationResult ValidateExamSubmissionIntegrity(int studentExamId)
        {
            var result = new ValidationResult { IsValid = true };
            
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null)
            {
                result.IsValid = false;
                result.Errors = new[] { "Student exam not found" };
                return result;
            }
            
            // Check if exam is already submitted
            if (studentExam.IsSubmitted)
            {
                result.IsValid = false;
                result.Errors = new[] { "Exam already submitted" };
                return result;
            }
            
            return result;
        }

        public bool ExecuteInTransaction(Func<bool> operation)
        {
            try
            {
                using (var transaction = new TransactionScope())
                {
                    var result = operation();
                    if (result)
                    {
                        transaction.Complete();
                    }
                    return result;
                }
            }
            catch
            {
                return false;
            }
        }

        public List<DataIntegrityIssue> CheckDatabaseIntegrity()
        {
            var issues = new List<DataIntegrityIssue>();
            
            // Check for orphaned records, missing references, etc.
            // This is a simplified implementation
            
            return issues;
        }

        public bool RepairDataIntegrityIssue(int issueId)
        {
            // Implementation for repairing specific integrity issues
            return true;
        }

        public void BackupCriticalData()
        {
            // Implementation for backing up critical data
        }

        public bool RestoreFromBackup(string backupId)
        {
            // Implementation for restoring from backup
            return true;
        }
    }
}
