using System.Collections.Generic;
using System.Linq;
using HEMS.Models;

namespace HEMS.Services
{
    public class ExamService : IExamService
    {
        private readonly HEMSContext _context;
        
        public ExamService(HEMSContext context) 
        { 
            _context = context; 
        }

        public List<Exam> GetAvailableExams(int studentId)
        {
            return _context.Exams.Where(e => e.IsPublished).ToList();
        }

        public Exam GetExamById(int examId)
        {
            return _context.Exams.Find(examId);
        }

        public List<Exam> GetAllExams()
        {
            return _context.Exams.ToList();
        }

        public List<Question> GetExamQuestions(int examId)
        {
            return _context.Questions.Where(q => q.ExamId == examId).OrderBy(q => q.QuestionOrder).ToList();
        }

        public Exam CreateExam(string title, int academicYear, int durationMinutes, DateTime examStartDateTime, DateTime examEndDateTime)
        {
            var exam = new Exam 
            { 
                Title = title, 
                AcademicYear = academicYear, 
                DurationMinutes = durationMinutes,
                ExamStartDateTime = examStartDateTime,
                ExamEndDateTime = examEndDateTime,
                IsPublished = false,
                CreatedDate = System.DateTime.Now
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();
            return exam;
        }

        public Question AddQuestion(int examId, string questionText, List<string> choiceTexts, int correctChoiceIndex)
        {
            var question = new Question 
            { 
                ExamId = examId, 
                QuestionText = questionText,
                QuestionOrder = _context.Questions.Count(q => q.ExamId == examId) + 1
            };
            _context.Questions.Add(question);
            _context.SaveChanges();

            for (int i = 0; i < choiceTexts.Count; i++)
            {
                var choice = new Choice
                {
                    QuestionId = question.QuestionId,
                    ChoiceText = choiceTexts[i],
                    IsCorrect = i == correctChoiceIndex,
                    ChoiceOrder = i + 1
                };
                _context.Choices.Add(choice);
            }
            _context.SaveChanges();
            return question;
        }

        public bool PublishExam(int examId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null) return false;
            
            // Check if exam has questions
            var hasQuestions = _context.Questions.Any(q => q.ExamId == examId);
            if (!hasQuestions) return false;
            
            exam.IsPublished = true;
            _context.SaveChanges();
            return true;
        }

        public EnhancedExamValidationResult ValidateExamAccessEnhanced(int examId, int studentId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.ExamNotFound, "Exam not found");
            }
            
            if (!exam.IsPublished)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.ExamNotPublished, "Exam not published");
            }
            
            var student = _context.Students.Find(studentId);
            if (student == null)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.UnauthorizedAccess, "Student not found");
            }
            
            return EnhancedExamValidationResult.Success();
        }

        public EnhancedExamValidationResult ValidateAnswerSubmissionEnhanced(int studentExamId, int questionId, int? choiceId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.SessionTimeout, "Student exam session not found");
            }
            
            if (studentExam.IsSubmitted)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.ExamAlreadyCompleted, "Exam already submitted");
            }
            
            var question = _context.Questions.Find(questionId);
            if (question == null || question.ExamId != studentExam.ExamId)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.InvalidQuestionNavigation, "Invalid question");
            }
            
            return EnhancedExamValidationResult.Success();
        }

        public EnhancedExamValidationResult ValidateExamSubmissionEnhanced(int studentExamId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.SessionTimeout, "Student exam session not found");
            }
            
            if (studentExam.IsSubmitted)
            {
                return EnhancedExamValidationResult.Failure(ExamAccessErrorType.ExamAlreadyCompleted, "Exam already submitted");
            }
            
            return EnhancedExamValidationResult.Success();
        }

        // Legacy methods for backward compatibility
        public StudentExam StartExam(int studentId, int examId)
        {
            var studentExam = new StudentExam 
            { 
                StudentId = studentId, 
                ExamId = examId, 
                StartDateTime = System.DateTime.Now 
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();
            return studentExam;
        }

        public bool SaveAnswer(int studentExamId, int questionId, int choiceId)
        {
            var answer = _context.StudentAnswers.FirstOrDefault(sa => sa.StudentExamId == studentExamId && sa.QuestionId == questionId);
            if (answer == null)
            {
                answer = new StudentAnswer 
                { 
                    StudentExamId = studentExamId, 
                    QuestionId = questionId, 
                    ChoiceId = choiceId,
                    LastModified = System.DateTime.Now
                };
                _context.StudentAnswers.Add(answer);
            }
            else
            {
                answer.ChoiceId = choiceId;
                answer.LastModified = System.DateTime.Now;
            }
            _context.SaveChanges();
            return true;
        }

        public bool FlagQuestion(int studentExamId, int questionId, bool isFlagged)
        {
            var answer = _context.StudentAnswers.FirstOrDefault(sa => sa.StudentExamId == studentExamId && sa.QuestionId == questionId);
            if (answer == null)
            {
                answer = new StudentAnswer 
                { 
                    StudentExamId = studentExamId, 
                    QuestionId = questionId, 
                    IsFlagged = isFlagged,
                    LastModified = System.DateTime.Now
                };
                _context.StudentAnswers.Add(answer);
            }
            else
            {
                answer.IsFlagged = isFlagged;
                answer.LastModified = System.DateTime.Now;
            }
            _context.SaveChanges();
            return true;
        }

        public bool SubmitExam(int studentExamId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null || studentExam.IsSubmitted) return false;
            
            studentExam.IsSubmitted = true;
            studentExam.SubmitDateTime = System.DateTime.Now;
            _context.SaveChanges();
            return true;
        }

        public StudentExam GetStudentExamSession(int studentExamId)
        {
            return _context.StudentExams.Find(studentExamId);
        }

        public StudentAnswer GetStudentAnswer(int studentExamId, int questionId)
        {
            return _context.StudentAnswers.FirstOrDefault(sa => sa.StudentExamId == studentExamId && sa.QuestionId == questionId);
        }

        public bool IsExamAvailable(int examId, int studentId)
        {
            var exam = _context.Exams.Find(examId);
            if (exam == null || !exam.IsPublished) return false;
            
            var student = _context.Students.Find(studentId);
            if (student == null) return false;
            
            var user = _context.Users.Find(student.UserId);
            return user != null && user.LoginPhaseCompleted;
        }
    }
}
