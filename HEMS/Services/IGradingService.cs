using System.Collections.Generic;
using HEMS.Models;

namespace HEMS.Services
{
    public interface IGradingService
    {
        double CalculateScore(StudentExam studentExam);
        GradingResult GradeExam(int studentExamId);
        GradingResult GetGradingResult(int studentExamId);
        void UpdateGrade(int studentExamId, double score);
        List<GradingResult> GradeAllSubmissions(int examId);
        bool ValidateGradingCriteria(Exam exam);
    }
}