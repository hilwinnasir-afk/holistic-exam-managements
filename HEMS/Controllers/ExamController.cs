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
        /// Start an exam - Create student exam session and redirect to real exam
        /// </summary>
        [HttpPost]
        public ActionResult Start(int examId = 1)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Starting real exam for examId: {examId}");
                
                // Get the exam
                var exam = _context.Exams
                    .Include(e => e.Questions)
                    .ThenInclude(q => q.Choices)
                    .FirstOrDefault(e => e.ExamId == examId);

                if (exam == null)
                {
                    TempData["ErrorMessage"] = "Exam not found.";
                    return RedirectToAction("ExamAccess", "Home");
                }

                if (!exam.IsPublished)
                {
                    TempData["ErrorMessage"] = "This exam is not yet available.";
                    return RedirectToAction("ExamAccess", "Home");
                }

                // For demo purposes, create a dummy student (in real implementation, get from session)
                var student = _context.Students.FirstOrDefault();
                if (student == null)
                {
                    // Create a demo student if none exists
                    var demoUser = new User
                    {
                        Username = "demo@university.edu.et",
                        PasswordHash = "demo_hash",
                        RoleId = 2, // Student role
                        LoginPhaseCompleted = true,
                        MustChangePassword = false,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Users.Add(demoUser);
                    _context.SaveChanges();

                    student = new Student
                    {
                        UserId = demoUser.UserId,
                        IdNumber = "ST001",
                        UniversityEmail = "demo@university.edu.et",
                        FirstName = "Demo",
                        LastName = "Student",
                        Email = "demo@university.edu.et",
                        BatchYear = "2025",
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Students.Add(student);
                    _context.SaveChanges();
                }

                // Check if student already has an exam session for this exam
                var existingSession = _context.StudentExams
                    .FirstOrDefault(se => se.StudentId == student.StudentId && se.ExamId == examId);

                if (existingSession != null)
                {
                    if (existingSession.IsSubmitted)
                    {
                        TempData["InfoMessage"] = "You have already submitted this exam.";
                        return RedirectToAction("Result", new { studentExamId = existingSession.StudentExamId });
                    }
                    else
                    {
                        // Continue existing session
                        return RedirectToAction("Take", new { studentExamId = existingSession.StudentExamId });
                    }
                }

                // Create new exam session
                var studentExam = new StudentExam
                {
                    StudentId = student.StudentId,
                    ExamId = examId,
                    StartDateTime = DateTime.UtcNow,
                    IsSubmitted = false
                };

                _context.StudentExams.Add(studentExam);
                _context.SaveChanges();

                // Create empty answer records for all questions
                foreach (var question in exam.Questions)
                {
                    var studentAnswer = new StudentAnswer
                    {
                        StudentExamId = studentExam.StudentExamId,
                        QuestionId = question.QuestionId,
                        ChoiceId = null,
                        IsFlagged = false,
                        LastModified = DateTime.UtcNow
                    };
                    _context.StudentAnswers.Add(studentAnswer);
                }
                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Created student exam session: {studentExam.StudentExamId}");
                
                // Redirect to real exam taking interface
                return RedirectToAction("Take", new { studentExamId = studentExam.StudentExamId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error starting exam: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
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
        /// Take exam - Display real exam with all coordinator-created content
        /// </summary>
        public ActionResult Take(int studentExamId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Loading exam for studentExamId: {studentExamId}");

                // Get student exam session with all related data
                var studentExam = _context.StudentExams
                    .Include(se => se.Exam)
                    .ThenInclude(e => e.Questions.OrderBy(q => q.QuestionOrder))
                    .ThenInclude(q => q.Choices.OrderBy(c => c.ChoiceOrder))
                    .Include(se => se.Student)
                    .ThenInclude(s => s.User)
                    .FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ERROR] Student exam session not found: {studentExamId}");
                    TempData["ErrorMessage"] = "Exam session not found.";
                    return RedirectToAction("ExamAccess", "Home");
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Found exam: {studentExam.Exam.Title} with {studentExam.Exam.Questions.Count} questions");

                // Check if exam is already submitted
                if (studentExam.IsSubmitted)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Exam already submitted, redirecting to results");
                    TempData["InfoMessage"] = "This exam has already been submitted.";
                    return RedirectToAction("Result", new { studentExamId = studentExamId });
                }

                // Check if exam time has expired
                var examEndTime = studentExam.StartDateTime.AddMinutes(studentExam.Exam.DurationMinutes);
                if (DateTime.UtcNow > examEndTime)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Exam time expired, auto-submitting");
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

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Found {studentAnswers.Count} student answers");

                // Calculate remaining time
                var remainingMinutes = (int)Math.Max(0, (examEndTime - DateTime.UtcNow).TotalMinutes);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Remaining time: {remainingMinutes} minutes");

                // Prepare view data
                ViewBag.StudentExam = studentExam;
                ViewBag.StudentAnswers = studentAnswers;
                ViewBag.RemainingTimeMinutes = remainingMinutes;

                // Log exam details for debugging
                foreach (var question in studentExam.Exam.Questions.OrderBy(q => q.QuestionOrder))
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Question {question.QuestionOrder}: {question.QuestionText.Substring(0, Math.Min(50, question.QuestionText.Length))}...");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] - Has {question.Choices.Count} choices");
                }

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error loading exam: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
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
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Submitting exam for studentExamId: {studentExamId}");

                // Get student exam session with all related data
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

                if (studentExam.IsSubmitted)
                {
                    TempData["InfoMessage"] = "This exam has already been submitted.";
                    return RedirectToAction("Result", new { studentExamId = studentExamId });
                }

                // Get student answers
                var studentAnswers = _context.StudentAnswers
                    .Where(sa => sa.StudentExamId == studentExamId)
                    .ToList();

                // Calculate score
                int correctAnswers = 0;
                int totalQuestions = studentExam.Exam.Questions.Count;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Grading exam with {totalQuestions} questions");

                foreach (var question in studentExam.Exam.Questions)
                {
                    var studentAnswer = studentAnswers.FirstOrDefault(sa => sa.QuestionId == question.QuestionId);
                    var correctChoice = question.Choices.FirstOrDefault(c => c.IsCorrect);
                    
                    if (studentAnswer != null && correctChoice != null && studentAnswer.ChoiceId == correctChoice.ChoiceId)
                    {
                        correctAnswers++;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Question {question.QuestionOrder}: CORRECT");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Question {question.QuestionOrder}: INCORRECT or UNANSWERED");
                    }
                }

                // Calculate percentage
                decimal percentage = totalQuestions > 0 ? (decimal)(correctAnswers * 100) / totalQuestions : 0;

                // Submit exam and save score
                studentExam.IsSubmitted = true;
                studentExam.SubmitDateTime = DateTime.UtcNow;
                studentExam.Score = correctAnswers;
                studentExam.Percentage = percentage;

                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Exam submitted successfully. Score: {correctAnswers}/{totalQuestions} ({percentage:F1}%)");

                TempData["SuccessMessage"] = "Exam submitted successfully!";
                return RedirectToAction("Result", new { studentExamId = studentExamId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error submitting exam: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while submitting the exam. Please try again.";
                return RedirectToAction("Take", new { studentExamId = studentExamId });
            }
        }

        /// <summary>
        /// Show result - display score and percentage with detailed breakdown
        /// </summary>
        public ActionResult Result(int studentExamId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Loading results for studentExamId: {studentExamId}");

                // Get student exam session with all related data
                var studentExam = _context.StudentExams
                    .Include(se => se.Exam)
                    .ThenInclude(e => e.Questions.OrderBy(q => q.QuestionOrder))
                    .ThenInclude(q => q.Choices.OrderBy(c => c.ChoiceOrder))
                    .Include(se => se.Student)
                    .ThenInclude(s => s.User)
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

                // Get student answers
                var studentAnswers = _context.StudentAnswers
                    .Where(sa => sa.StudentExamId == studentExamId)
                    .ToList();

                // If scores weren't calculated during submission, calculate them now
                if (studentExam.Score == null || studentExam.Percentage == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Calculating scores for result display");

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

                    // Update the database with calculated scores
                    studentExam.Score = correctAnswers;
                    studentExam.Percentage = percentage;
                    _context.SaveChanges();
                }

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Results - Score: {studentExam.Score}, Percentage: {studentExam.Percentage}%");

                // Prepare view data
                ViewBag.StudentExam = studentExam;
                ViewBag.StudentAnswers = studentAnswers;
                ViewBag.CorrectAnswers = (int)(studentExam.Score ?? 0);
                ViewBag.TotalQuestions = studentExam.Exam.Questions.Count;
                ViewBag.Percentage = (int)(studentExam.Percentage ?? 0);

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error loading results: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
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