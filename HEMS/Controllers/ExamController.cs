using System;
using System.Linq;
using HEMS.Models;
using HEMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HEMS.Attributes;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Controllers
{
    public class ExamController : Controller
    {
        private readonly HEMSContext _context;
        private readonly IExamService _examService;
        private readonly ISessionService _sessionService;

        public ExamController(HEMSContext context, IExamService examService, ISessionService sessionService)
        {
            _context = context;
            _examService = examService;
            _sessionService = sessionService;
        }

        /// <summary>
        /// Start an exam - DEMO MODE: Redirect directly to mock exam
        /// </summary>
        [HttpPost]
        public ActionResult Start(int examId = 1)
        {
            try
            {
                // DEMO MODE: Go directly to mock exam without database operations
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Starting mock exam demo mode for examId: {examId}");
                
                // Redirect to mock exam interface
                return RedirectToAction("MockExam");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error starting exam: {ex.Message}");
                TempData["ErrorMessage"] = $"An error occurred while starting the exam. Please try again.";
                return RedirectToAction("ExamAccess", "Home");
            }
        }

        /// <summary>
        /// Mock Exam - Simple demo exam interface without database dependencies
        /// GET: /Exam/MockExam
        /// </summary>
        public ActionResult MockExam()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading MockExam action");
                
                // Create mock exam data - completely independent of database
                var mockExam = new
                {
                    Title = "Computer Science Fundamentals - Demo Exam",
                    AcademicYear = "2025-2026",
                    DurationMinutes = 60,
                    Questions = new[]
                    {
                        new {
                            QuestionId = 1,
                            QuestionText = "What is the time complexity of binary search algorithm?",
                            Choices = new[]
                            {
                                new { ChoiceId = 1, ChoiceText = "O(n)", IsCorrect = false },
                                new { ChoiceId = 2, ChoiceText = "O(log n)", IsCorrect = true },
                                new { ChoiceId = 3, ChoiceText = "O(n²)", IsCorrect = false },
                                new { ChoiceId = 4, ChoiceText = "O(1)", IsCorrect = false }
                            }
                        },
                        new {
                            QuestionId = 2,
                            QuestionText = "Which data structure uses LIFO (Last In, First Out) principle?",
                            Choices = new[]
                            {
                                new { ChoiceId = 5, ChoiceText = "Queue", IsCorrect = false },
                                new { ChoiceId = 6, ChoiceText = "Stack", IsCorrect = true },
                                new { ChoiceId = 7, ChoiceText = "Array", IsCorrect = false },
                                new { ChoiceId = 8, ChoiceText = "Linked List", IsCorrect = false }
                            }
                        },
                        new {
                            QuestionId = 3,
                            QuestionText = "What does SQL stand for?",
                            Choices = new[]
                            {
                                new { ChoiceId = 9, ChoiceText = "Structured Query Language", IsCorrect = true },
                                new { ChoiceId = 10, ChoiceText = "Simple Query Language", IsCorrect = false },
                                new { ChoiceId = 11, ChoiceText = "Standard Query Language", IsCorrect = false },
                                new { ChoiceId = 12, ChoiceText = "System Query Language", IsCorrect = false }
                            }
                        },
                        new {
                            QuestionId = 4,
                            QuestionText = "Which of the following is NOT a programming paradigm?",
                            Choices = new[]
                            {
                                new { ChoiceId = 13, ChoiceText = "Object-Oriented Programming", IsCorrect = false },
                                new { ChoiceId = 14, ChoiceText = "Functional Programming", IsCorrect = false },
                                new { ChoiceId = 15, ChoiceText = "Procedural Programming", IsCorrect = false },
                                new { ChoiceId = 16, ChoiceText = "Database Programming", IsCorrect = true }
                            }
                        },
                        new {
                            QuestionId = 5,
                            QuestionText = "What is the primary purpose of version control systems like Git?",
                            Choices = new[]
                            {
                                new { ChoiceId = 17, ChoiceText = "To compile code", IsCorrect = false },
                                new { ChoiceId = 18, ChoiceText = "To track changes in source code", IsCorrect = true },
                                new { ChoiceId = 19, ChoiceText = "To debug applications", IsCorrect = false },
                                new { ChoiceId = 20, ChoiceText = "To deploy applications", IsCorrect = false }
                            }
                        }
                    }
                };

                // Pass mock data to view
                ViewBag.MockExam = mockExam;
                ViewBag.RemainingTimeMinutes = 60; // 60 minutes for demo
                ViewBag.StudentExamId = 999; // Mock ID for demo

                System.Diagnostics.Debug.WriteLine("[DEBUG] MockExam data prepared successfully");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error loading mock exam: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while loading the demo exam. Please try again.";
                return RedirectToAction("ExamAccess", "Home");
            }
        }

        /// <summary>
        /// Mock Result - Simple demo result page
        /// GET: /Exam/MockResult
        /// </summary>
        public ActionResult MockResult()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Loading MockResult action");
                
                // Create mock result data - completely independent of database
                ViewBag.ExamTitle = "Computer Science Fundamentals - Demo Exam";
                ViewBag.StudentName = "Demo Student";
                ViewBag.CorrectAnswers = 4;
                ViewBag.TotalQuestions = 5;
                ViewBag.Percentage = 80;
                ViewBag.Grade = "B";
                ViewBag.SubmissionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                System.Diagnostics.Debug.WriteLine("[DEBUG] MockResult data prepared successfully");
                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error loading mock results: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while loading demo results.";
                return RedirectToAction("ExamAccess", "Home");
            }
        }

        /// <summary>
        /// Take exam - DEMO MODE: Skip validations and show exam
        /// </summary>
        public ActionResult Take(int studentExamId)
        {
            try
            {
                // Get student exam session
                var studentExam = _context.StudentExams
                    .Include(se => se.Exam)
                    .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                    .FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null)
                {
                    TempData["ErrorMessage"] = "Exam session not found.";
                    return RedirectToAction("ExamAccess", "Home");
                }

                // Check if exam is already submitted
                if (studentExam.IsSubmitted)
                {
                    TempData["InfoMessage"] = "This exam has already been submitted.";
                    return RedirectToAction("Result", new { studentExamId = studentExamId });
                }

                // Check if exam time has expired
                var examEndTime = studentExam.StartDateTime.AddMinutes(studentExam.Exam.DurationMinutes);
                if (DateTime.UtcNow > examEndTime)
                {
                    // Auto-submit expired exam
                    studentExam.IsSubmitted = true;
                    studentExam.SubmitDateTime = DateTime.UtcNow;
                    _context.SaveChanges();

                    TempData["InfoMessage"] = "Exam time has expired. Your exam has been automatically submitted.";
                    return RedirectToAction("Result", new { studentExamId = studentExamId });
                }

                // Get student answers
                var studentAnswers = _context.StudentAnswers
                    .Where(sa => sa.StudentExamId == studentExamId)
                    .ToList();

                // Calculate remaining time
                var remainingMinutes = (int)(examEndTime - DateTime.UtcNow).TotalMinutes;

                // Prepare view model
                ViewBag.StudentExam = studentExam;
                ViewBag.StudentAnswers = studentAnswers;
                ViewBag.RemainingTimeMinutes = Math.Max(0, remainingMinutes);

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error loading exam: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the exam. Please try again.";
                return RedirectToAction("ExamAccess", "Home");
            }
        }

        /// <summary>
        /// Save answer - DEMO MODE: Skip validations and save answer
        /// </summary>
        [HttpPost]
        public JsonResult SaveAnswer(int studentExamId, int questionId, int choiceId)
        {
            try
            {
                // Verify session exists
                var studentExam = _context.StudentExams.FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null || studentExam.IsSubmitted)
                {
                    return Json(new { success = false, message = "Invalid session or exam already submitted" });
                }

                // Save or update answer
                var existingAnswer = _context.StudentAnswers
                    .FirstOrDefault(sa => sa.StudentExamId == studentExamId && sa.QuestionId == questionId);

                if (existingAnswer != null)
                {
                    existingAnswer.ChoiceId = choiceId;
                    existingAnswer.LastModified = DateTime.UtcNow;
                }
                else
                {
                    var newAnswer = new StudentAnswer
                    {
                        StudentExamId = studentExamId,
                        QuestionId = questionId,
                        ChoiceId = choiceId,
                        LastModified = DateTime.UtcNow
                    };
                    _context.StudentAnswers.Add(newAnswer);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Answer saved" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error saving answer: {ex.Message}");
                return Json(new { success = false, message = "Error saving answer" });
            }
        }

        /// <summary>
        /// Toggle flag - flag / unflag question
        /// </summary>
        [HttpPost]
        public JsonResult ToggleFlag(int studentExamId, int questionId, bool isFlagged)
        {
            try
            {
                // Verify session exists
                var studentExam = _context.StudentExams.FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null || studentExam.IsSubmitted)
                {
                    return Json(new { success = false, message = "Invalid session or exam already submitted" });
                }

                // Update or create answer record with flag
                var existingAnswer = _context.StudentAnswers
                    .FirstOrDefault(sa => sa.StudentExamId == studentExamId && sa.QuestionId == questionId);

                if (existingAnswer != null)
                {
                    existingAnswer.IsFlagged = isFlagged;
                    existingAnswer.LastModified = DateTime.UtcNow;
                }
                else
                {
                    var newAnswer = new StudentAnswer
                    {
                        StudentExamId = studentExamId,
                        QuestionId = questionId,
                        IsFlagged = isFlagged,
                        LastModified = DateTime.UtcNow
                    };
                    _context.StudentAnswers.Add(newAnswer);
                }

                _context.SaveChanges();
                return Json(new { success = true, message = isFlagged ? "Question flagged" : "Question unflagged" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error toggling flag: {ex.Message}");
                return Json(new { success = false, message = "Error updating flag" });
            }
        }

        /// <summary>
        /// Submit exam - final submission + auto grading
        /// </summary>
        [HttpPost]
        public ActionResult Submit(int studentExamId)
        {
            try
            {
                // Get student exam session
                var studentExam = _context.StudentExams
                    .Include(se => se.Exam)
                    .FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null)
                {
                    TempData["ErrorMessage"] = "Exam session not found.";
                    return RedirectToAction("ExamAccess", "Home");
                }

                if (studentExam.IsSubmitted)
                {
                    TempData["InfoMessage"] = "This exam has already been submitted.";
                    return RedirectToAction("Result", new { studentExamId = studentExamId });
                }

                // Submit exam
                studentExam.IsSubmitted = true;
                studentExam.SubmitDateTime = DateTime.UtcNow;
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Exam submitted successfully!";
                return RedirectToAction("Result", new { studentExamId = studentExamId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error submitting exam: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while submitting the exam. Please try again.";
                return RedirectToAction("Take", new { studentExamId = studentExamId });
            }
        }

        /// <summary>
        /// Show result - display score and percentage
        /// </summary>
        public ActionResult Result(int studentExamId)
        {
            try
            {
                // Get student exam session
                var studentExam = _context.StudentExams
                    .Include(se => se.Exam)
                    .ThenInclude(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                    .FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null)
                {
                    TempData["ErrorMessage"] = "Exam session not found.";
                    return RedirectToAction("ExamAccess", "Home");
                }

                // Check if exam is submitted
                if (!studentExam.IsSubmitted)
                {
                    TempData["ErrorMessage"] = "You must submit the exam before viewing results.";
                    return RedirectToAction("Take", new { studentExamId = studentExamId });
                }

                // Calculate score
                var studentAnswers = _context.StudentAnswers
                    .Where(sa => sa.StudentExamId == studentExamId)
                    .ToList();

                int correctAnswers = 0;
                int totalQuestions = studentExam.Exam.Questions.Count;

                foreach (var question in studentExam.Exam.Questions)
                {
                    var studentAnswer = studentAnswers.FirstOrDefault(sa => sa.QuestionId == question.QuestionId);
                    if (studentAnswer != null)
                    {
                        var correctChoice = question.Choices.FirstOrDefault(c => c.IsCorrect);
                        if (correctChoice != null && studentAnswer.ChoiceId == correctChoice.ChoiceId)
                        {
                            correctAnswers++;
                        }
                    }
                }

                var percentage = totalQuestions > 0 ? (correctAnswers * 100) / totalQuestions : 0;

                ViewBag.StudentExam = studentExam;
                ViewBag.StudentAnswers = studentAnswers;
                ViewBag.CorrectAnswers = correctAnswers;
                ViewBag.TotalQuestions = totalQuestions;
                ViewBag.Percentage = percentage;

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error loading results: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading results. Please try again.";
                return RedirectToAction("ExamAccess", "Home");
            }
        }

        /// <summary>
        /// Get remaining time for timer (AJAX endpoint)
        /// </summary>
        public JsonResult GetRemainingTime(int studentExamId)
        {
            try
            {
                var studentExam = _context.StudentExams
                    .Include(se => se.Exam)
                    .FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null || studentExam.IsSubmitted)
                {
                    return Json(new { success = false, message = "Invalid session or exam already submitted" });
                }

                var examEndTime = studentExam.StartDateTime.AddMinutes(studentExam.Exam.DurationMinutes);
                var remainingTime = examEndTime - DateTime.UtcNow;
                var totalSeconds = (int)Math.Max(0, remainingTime.TotalSeconds);

                var hours = totalSeconds / 3600;
                var minutes = (totalSeconds % 3600) / 60;
                var seconds = totalSeconds % 60;

                var formattedTime = hours > 0 
                    ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
                    : $"{minutes:D2}:{seconds:D2}";

                return Json(new { 
                    success = true, 
                    totalSeconds = totalSeconds,
                    formattedTime = formattedTime
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error getting remaining time: {ex.Message}");
                return Json(new { success = false, message = "Error getting remaining time" });
            }
        }

        /// <summary>
        /// Auto-submit exam when time expires
        /// </summary>
        public ActionResult AutoSubmitExam(int studentExamId)
        {
            try
            {
                var studentExam = _context.StudentExams.Find(studentExamId);
                if (studentExam != null && !studentExam.IsSubmitted)
                {
                    studentExam.IsSubmitted = true;
                    studentExam.SubmitDateTime = DateTime.UtcNow;
                    _context.SaveChanges();

                    TempData["InfoMessage"] = "Time expired. Your exam has been automatically submitted.";
                }

                return RedirectToAction("Result", new { studentExamId = studentExamId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error auto-submitting exam: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred during auto-submission.";
                return RedirectToAction("ExamAccess", "Home");
            }
        }
    }
}