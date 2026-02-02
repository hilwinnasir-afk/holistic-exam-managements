using System;
using System.Collections.Generic;
using HEMS.Models;

namespace HEMS.Services
{
    public interface IExamService
    {
        List<Exam> GetAvailableExams(int studentId);
        Exam GetExamById(int examId);
        List<Exam> GetAllExams();
        List<Question> GetExamQuestions(int examId);
        Exam CreateExam(string title, int academicYear, int durationMinutes, DateTime examStartDateTime, DateTime examEndDateTime);
        Question AddQuestion(int examId, string questionText, List<string> choiceTexts, int correctChoiceIndex);
        bool PublishExam(int examId);
        EnhancedExamValidationResult ValidateExamAccessEnhanced(int examId, int studentId);
        EnhancedExamValidationResult ValidateAnswerSubmissionEnhanced(int studentExamId, int questionId, int? choiceId);
        EnhancedExamValidationResult ValidateExamSubmissionEnhanced(int studentExamId);
    }
}
