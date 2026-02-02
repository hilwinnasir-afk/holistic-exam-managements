using System.Collections.Generic;
using System.Linq;
using HEMS.Models;
using System;

namespace HEMS.Services
{
    public class GradingService : IGradingService
    {
        private readonly HEMSContext _context;
        
        public GradingService(HEMSContext context) 
        { 
            _context = context; 
        }

        public double CalculateScore(StudentExam studentExam)
        {
            var questions = _context.Questions.Where(q => q.ExamId == studentExam.ExamId).ToList();
            var answers = _context.StudentAnswers.Where(a => a.StudentExamId == studentExam.StudentExamId).ToList();
            
            if (!questions.Any()) return 0;
            
            int correctAnswers = 0;
            foreach (var question in questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);
                if (answer?.ChoiceId.HasValue == true)
                {
                    var choice = _context.Choices.Find(answer.ChoiceId.Value);
                    if (choice?.IsCorrect == true)
                    {
                        correctAnswers++;
                    }
                }
            }
            
            return (double)correctAnswers / questions.Count * 100;
        }

        public GradingResult GradeExam(int studentExamId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null) return null;
            
            var result = GetGradingResult(studentExamId);
            
            // Update the student exam with the calculated score
            studentExam.Score = (decimal?)result.CorrectAnswers;
            studentExam.Percentage = (decimal?)result.Percentage;
            _context.SaveChanges();
            
            return result;
        }

        public GradingResult GetGradingResult(int studentExamId)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam == null) return null;
            
            var questions = _context.Questions.Where(q => q.ExamId == studentExam.ExamId).ToList();
            var answers = _context.StudentAnswers.Where(a => a.StudentExamId == studentExamId).ToList();
            
            int totalQuestions = questions.Count;
            int correctAnswers = 0;
            int incorrectAnswers = 0;
            
            foreach (var question in questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);
                if (answer?.ChoiceId.HasValue == true)
                {
                    var choice = _context.Choices.Find(answer.ChoiceId.Value);
                    if (choice?.IsCorrect == true)
                    {
                        correctAnswers++;
                    }
                    else
                    {
                        incorrectAnswers++;
                    }
                }
            }
            
            int unansweredQuestions = totalQuestions - (correctAnswers + incorrectAnswers);
            double percentage = totalQuestions == 0 ? 0.0 : Math.Round((double)correctAnswers / totalQuestions * 100.0, 2);
            
            return new GradingResult
            {
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = incorrectAnswers,
                UnansweredQuestions = unansweredQuestions,
                Percentage = percentage,
                GradedDateTime = DateTime.Now
            };
        }

        public void UpdateGrade(int studentExamId, double score)
        {
            var studentExam = _context.StudentExams.Find(studentExamId);
            if (studentExam != null)
            {
                studentExam.Percentage = (decimal?)score;
                _context.SaveChanges();
            }
        }

        public List<GradingResult> GradeAllSubmissions(int examId)
        {
            var studentExams = _context.StudentExams.Where(se => se.ExamId == examId && se.IsSubmitted).ToList();
            var results = new List<GradingResult>();
            
            foreach (var studentExam in studentExams)
            {
                var result = GetGradingResult(studentExam.StudentExamId);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            
            return results;
        }

        public bool ValidateGradingCriteria(Exam exam)
        {
            if (exam == null) return false;
            
            // Check if exam has questions with correct answers
            var questions = _context.Questions.Where(q => q.ExamId == exam.ExamId).ToList();
            if (!questions.Any()) return false;
            
            foreach (var question in questions)
            {
                var hasCorrectAnswer = _context.Choices.Any(c => c.QuestionId == question.QuestionId && c.IsCorrect);
                if (!hasCorrectAnswer) return false;
            }
            
            return true;
        }
    }
}
