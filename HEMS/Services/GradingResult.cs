using System;

namespace HEMS.Services
{
    /// <summary>
    /// Result of exam grading operation
    /// </summary>
    public class GradingResult
    {
        public int StudentExamId { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnansweredQuestions { get; set; }
        public double Percentage { get; set; }
        public string Grade { get; set; } = string.Empty;
        public DateTime GradedDateTime { get; set; }
        public bool IsGraded { get; set; }
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// Creates a successful grading result
        /// </summary>
        public static GradingResult Success(int studentExamId, int totalQuestions, int correctAnswers)
        {
            var percentage = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 100 : 0;
            var grade = CalculateGrade(percentage);

            return new GradingResult
            {
                StudentExamId = studentExamId,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = totalQuestions - correctAnswers,
                UnansweredQuestions = 0,
                Percentage = Math.Round(percentage, 2),
                Grade = grade,
                GradedDateTime = DateTime.Now,
                IsGraded = true
            };
        }

        /// <summary>
        /// Creates a failed grading result
        /// </summary>
        public static GradingResult Failure(int studentExamId, string errorMessage)
        {
            return new GradingResult
            {
                StudentExamId = studentExamId,
                IsGraded = false,
                Comments = errorMessage,
                GradedDateTime = DateTime.Now
            };
        }

        private static string CalculateGrade(double percentage)
        {
            return percentage switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }
    }
}