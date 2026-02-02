using HEMS.Attributes;
using HEMS.Models;
using HEMS.Models.ViewModels;
using HEMS.Services;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace HEMS.Controllers
{
    /// <summary>
    /// Controller for coordinator-specific functionality including student management
    /// </summary>
    [CoordinatorAuthorize]
    public class CoordinatorController : Controller
    {
        private readonly HEMSContext _context;
        private readonly ISessionService _sessionService;
        private readonly IExamService _examService;
        private readonly IValidationService _validationService;
        private readonly IDatabaseOptimizationService _optimizationService;

        public CoordinatorController(HEMSContext context, ISessionService sessionService, IExamService examService, IValidationService validationService, IDatabaseOptimizationService optimizationService)
        {
            _context = context;
            _sessionService = sessionService;
            _examService = examService;
            _validationService = validationService;
            _optimizationService = optimizationService;
        }

        /// <summary>
        /// Main coordinator dashboard
        /// </summary>
        public ActionResult Index()
        {
            var currentUser = _sessionService.GetCurrentUser();
            ViewBag.User = currentUser;

            // Get statistics for dashboard
            ViewBag.TotalStudents = _context.Students.Count();
            ViewBag.TotalExams = _context.Exams.Count();
            ViewBag.PublishedExams = _context.Exams.Count(e => e.IsPublished);
            ViewBag.ActiveExamSessions = _context.ExamSessions.Count(es => es.IsActive);

            return View();
        }

        #region Student Management

        /// <summary>
        /// Display student import form
        /// </summary>
        public ActionResult ImportStudents()
        {
            var model = new StudentImportViewModel
            {
                BatchYear = "Year IV Sem II" // Default batch year as string
            };
            return View(model);
        }

        /// <summary>
        /// Process student import from CSV/Excel file
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportStudents(StudentImportViewModel model)
        {
            try
            {
                // Comprehensive validation using ValidationService
                var validationResult = _validationService.ValidateStudentImport(model);
                
                if (!validationResult.IsValid)
                {
                    // Add validation errors to ModelState
                    foreach (var error in validationResult.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }

                    // Separate errors and warnings for display
                    model.ValidationErrors = validationResult.Errors.ToList();
                    model.ValidationWarnings = validationResult.Warnings.ToList();

                    return View(model);
                }

                // Process the import file
                var importResults = ProcessImportFile(model.ImportFile, model.BatchYear, model.SkipHeaderRow);
                
                var summary = new ImportSummaryViewModel
                {
                    TotalRecords = importResults.Count,
                    SuccessfulImports = importResults.Count(r => r.IsSuccess),
                    FailedImports = importResults.Count(r => !r.IsSuccess),
                    Results = importResults
                };

                return View("ImportSummary", summary);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error processing import file: {ex.Message}");
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"ImportStudents Error: {ex.Message}");
                
                return View(model);
            }
        }

        /// <summary>
        /// Display list of all students
        /// </summary>
        public ActionResult ManageStudents()
        {
            var students = _context.Students
                .Include("User")
                .OrderBy(s => s.IdNumber)
                .ToList();

            return View(students);
        }

        /// <summary>
        /// Delete a student record
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStudent(int studentId)
        {
            try
            {
                var student = _context.Students
                    .Include("User")
                    .FirstOrDefault(s => s.StudentId == studentId);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found";
                    return RedirectToAction("ManageStudents");
                }

                // Check if student has taken any exams
                var hasExams = _context.StudentExams.Any(se => se.StudentId == studentId);
                if (hasExams)
                {
                    TempData["ErrorMessage"] = "Cannot delete student who has taken exams";
                    return RedirectToAction("ManageStudents");
                }

                // Delete student and associated user
                var user = student.User;
                _context.Students.Remove(student);
                if (user != null)
                {
                    _context.Users.Remove(user);
                }

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Student deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting student: {ex.Message}";
            }

            return RedirectToAction("ManageStudents");
        }

        /// <summary>
        /// Add individual student
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddIndividualStudent(StudentImportModel model)
        {
            try
            {
                // Validate the model
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fill in all required fields correctly.";
                    return RedirectToAction("ImportStudents");
                }

                // Check if student with this ID or email already exists
                var existingStudent = _context.Students
                    .FirstOrDefault(s => s.IdNumber == model.IdNumber || s.UniversityEmail == model.UniversityEmail);

                if (existingStudent != null)
                {
                    TempData["ErrorMessage"] = "A student with this ID number or email already exists.";
                    return RedirectToAction("ImportStudents");
                }

                // Create the student record
                var createdIds = CreateStudentRecord(model);
                if (createdIds.HasValue)
                {
                    TempData["SuccessMessage"] = $"Student '{model.StudentName}' added successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create student record. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error adding student: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AddIndividualStudent Error: {ex.Message}");
            }

            return RedirectToAction("ImportStudents");
        }

        #endregion

        #region Exam Management

        /// <summary>
        /// Display exam creation form
        /// </summary>
        public ActionResult CreateExam()
        {
            var model = new ExamCreateViewModel
            {
                AcademicYear = DateTime.Now.Year,
                DurationMinutes = 120 // Default 2 hours
            };
            return View(model);
        }

        /// <summary>
        /// Process exam creation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateExam(ExamCreateViewModel model)
        {
            try
            {
                // Add custom validation for datetime fields
                if (model.ExamEndDateTime <= model.ExamStartDateTime)
                {
                    if (model.ExamEndDateTime.Date == model.ExamStartDateTime.Date)
                    {
                        ModelState.AddModelError("ExamEndDateTime", 
                            "Exam end time must be after the start time. Same-day exams are allowed with different times.");
                    }
                    else
                    {
                        ModelState.AddModelError("ExamEndDateTime", 
                            "Exam end date and time must be after the start date and time.");
                    }
                }

                if (model.ExamStartDateTime <= DateTime.Now)
                {
                    ModelState.AddModelError("ExamStartDateTime", "Exam start date and time must be in the future.");
                }

                // Comprehensive validation using ValidationService
                var validationResult = _validationService.ValidateExamCreation(model);
                
                if (!validationResult.IsValid)
                {
                    // Add validation errors to ModelState
                    foreach (var error in validationResult.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }

                    // Separate errors and warnings for display
                    model.ValidationErrors = validationResult.Errors.ToList();
                    model.ValidationWarnings = validationResult.Warnings.ToList();

                    return View(model);
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Create the exam with datetime fields
                var exam = _examService.CreateExam(model.Title, model.AcademicYear, model.DurationMinutes, 
                    model.ExamStartDateTime, model.ExamEndDateTime);
                TempData["SuccessMessage"] = $"Exam '{exam.Title}' created successfully for academic year {exam.AcademicYear}";
                return RedirectToAction("ManageExam", new { examId = exam.ExamId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating exam: {ex.Message}");
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"CreateExam Error: {ex.Message}");
                
                return View(model);
            }
        }

        /// <summary>
        /// Display exam management interface
        /// </summary>
        public ActionResult ManageExam(int examId)
        {
            var exam = _examService.GetExamById(examId);
            if (exam == null)
            {
                TempData["ErrorMessage"] = "Exam not found";
                return RedirectToAction("ListExams");
            }

            var questions = _examService.GetExamQuestions(examId);
            
            var model = new ExamManageViewModel
            {
                Exam = exam,
                Questions = questions,
                TotalQuestions = questions.Count
            };

            return View(model);
        }

        /// <summary>
        /// Display list of all exams
        /// </summary>
        public ActionResult ListExams()
        {
            var exams = _examService.GetAllExams();
            return View(exams);
        }

        /// <summary>
        /// Add question to exam
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddQuestion(QuestionCreateViewModel model)
        {
            try
            {
                // Comprehensive validation using ValidationService
                var validationResult = _validationService.ValidateQuestionCreation(model);
                
                if (!validationResult.IsValid)
                {
                    // Add validation errors to TempData for display
                    var errorMessages = validationResult.Errors.ToList();
                    var warningMessages = validationResult.Warnings.ToList();

                    if (errorMessages.Any())
                    {
                        TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);
                    }

                    if (warningMessages.Any())
                    {
                        TempData["WarningMessage"] = string.Join("<br/>", warningMessages);
                    }

                    return RedirectToAction("ManageExam", new { examId = model.ExamId });
                }

                // Create choice list
                var choiceTexts = new List<string>();
                if (!string.IsNullOrWhiteSpace(model.Choice1)) choiceTexts.Add(model.Choice1.Trim());
                if (!string.IsNullOrWhiteSpace(model.Choice2)) choiceTexts.Add(model.Choice2.Trim());
                if (!string.IsNullOrWhiteSpace(model.Choice3)) choiceTexts.Add(model.Choice3.Trim());
                if (!string.IsNullOrWhiteSpace(model.Choice4)) choiceTexts.Add(model.Choice4.Trim());

                // Add the question
                var question = _examService.AddQuestion(model.ExamId, model.QuestionText, choiceTexts, model.CorrectChoiceIndex);
                TempData["SuccessMessage"] = "Question added successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error adding question: {ex.Message}";
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"AddQuestion Error: {ex.Message}");
            }

            return RedirectToAction("ManageExam", new { examId = model.ExamId });
        }

        /// <summary>
        /// Add bulk questions to exam
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBulkQuestions(BulkQuestionEntryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please provide questions text.";
                    return RedirectToAction("ManageExam", new { examId = model.ExamId });
                }

                // Debug logging
                System.Diagnostics.Debug.WriteLine($"AddBulkQuestions: Received text with {model.QuestionsText?.Length ?? 0} characters");
                System.Diagnostics.Debug.WriteLine($"AddBulkQuestions: First 200 chars: {model.QuestionsText?.Substring(0, Math.Min(200, model.QuestionsText?.Length ?? 0))}");

                // Parse the bulk questions text
                var parsedQuestions = ParseBulkQuestions(model.QuestionsText);
                
                System.Diagnostics.Debug.WriteLine($"AddBulkQuestions: Parsed {parsedQuestions.Count} questions");
                
                if (!parsedQuestions.Any())
                {
                    TempData["ErrorMessage"] = "No valid questions found in the provided text. Please check the format.";
                    return RedirectToAction("ManageExam", new { examId = model.ExamId });
                }

                int successCount = 0;
                int errorCount = 0;
                var errors = new List<string>();

                // Add each parsed question
                foreach (var parsedQuestion in parsedQuestions)
                {
                    if (parsedQuestion.IsValid)
                    {
                        try
                        {
                            _examService.AddQuestion(model.ExamId, parsedQuestion.QuestionText, 
                                parsedQuestion.Choices, parsedQuestion.CorrectChoiceIndex);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            errors.Add($"Error adding question: {ex.Message}");
                        }
                    }
                    else
                    {
                        errorCount++;
                        errors.Add(parsedQuestion.ErrorMessage);
                    }
                }

                if (successCount > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully added {successCount} questions to the exam.";
                }

                if (errorCount > 0)
                {
                    TempData["ErrorMessage"] = $"{errorCount} questions failed to add. " + string.Join(" ", errors.Take(3));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing bulk questions: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"AddBulkQuestions Error: {ex.Message}");
            }

            return RedirectToAction("ManageExam", new { examId = model.ExamId });
        }

        /// <summary>
        /// Parse bulk questions text into individual questions
        /// Format: Q1. Question text? A. Choice 1 B. Choice 2 C. Choice 3 D. Choice 4 Correct: A
        /// </summary>
        private List<ParsedQuestion> ParseBulkQuestions(string questionsText)
        {
            var questions = new List<ParsedQuestion>();
            
            if (string.IsNullOrWhiteSpace(questionsText))
                return questions;

            // Split by question numbers (Q1., Q2., etc.)
            var questionBlocks = System.Text.RegularExpressions.Regex.Split(questionsText, @"Q\d+\.")
                .Where(block => !string.IsNullOrWhiteSpace(block))
                .ToList();

            // Debug logging
            System.Diagnostics.Debug.WriteLine($"ParseBulkQuestions: Found {questionBlocks.Count} question blocks");
            for (int debugIndex = 0; debugIndex < questionBlocks.Count; debugIndex++)
            {
                System.Diagnostics.Debug.WriteLine($"Block {debugIndex + 1}: {questionBlocks[debugIndex].Substring(0, Math.Min(100, questionBlocks[debugIndex].Length))}...");
            }

            for (int i = 0; i < questionBlocks.Count; i++)
            {
                var block = questionBlocks[i].Trim();
                var question = new ParsedQuestion();

                try
                {
                    // Extract question text (everything before first choice)
                    var choiceMatch = System.Text.RegularExpressions.Regex.Match(block, @"[A-Z]\.");
                    if (!choiceMatch.Success)
                    {
                        question.ErrorMessage = $"Question {i + 1}: No choices found";
                        question.IsValid = false;
                        questions.Add(question);
                        continue;
                    }

                    question.QuestionText = block.Substring(0, choiceMatch.Index).Trim();
                    if (string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        question.ErrorMessage = $"Question {i + 1}: Empty question text";
                        question.IsValid = false;
                        questions.Add(question);
                        continue;
                    }

                    // Extract choices and correct answer
                    var choicesSection = block.Substring(choiceMatch.Index);
                    
                    // Find correct answer
                    var correctMatch = System.Text.RegularExpressions.Regex.Match(choicesSection, @"Correct:\s*([A-Z])", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (!correctMatch.Success)
                    {
                        question.ErrorMessage = $"Question {i + 1}: No correct answer specified";
                        question.IsValid = false;
                        questions.Add(question);
                        continue;
                    }

                    var correctLetter = correctMatch.Groups[1].Value.ToUpper();
                    
                    // Remove the "Correct:" part to get clean choices
                    var choicesOnly = choicesSection.Substring(0, correctMatch.Index);
                    
                    // Extract individual choices
                    var choiceMatches = System.Text.RegularExpressions.Regex.Matches(choicesOnly, @"([A-Z])\.\s*([^A-Z]*?)(?=[A-Z]\.|$)");
                    
                    var choiceLetters = new List<string>();
                    foreach (System.Text.RegularExpressions.Match match in choiceMatches)
                    {
                        var letter = match.Groups[1].Value;
                        var text = match.Groups[2].Value.Trim();
                        
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            question.Choices.Add(text);
                            choiceLetters.Add(letter);
                        }
                    }

                    if (question.Choices.Count < 2)
                    {
                        question.ErrorMessage = $"Question {i + 1}: At least 2 choices required";
                        question.IsValid = false;
                        questions.Add(question);
                        continue;
                    }

                    // Find correct choice index
                    var correctIndex = choiceLetters.IndexOf(correctLetter);
                    if (correctIndex == -1)
                    {
                        question.ErrorMessage = $"Question {i + 1}: Correct answer '{correctLetter}' not found in choices";
                        question.IsValid = false;
                        questions.Add(question);
                        continue;
                    }

                    question.CorrectChoiceIndex = correctIndex;
                    question.IsValid = true;
                }
                catch (Exception ex)
                {
                    question.ErrorMessage = $"Question {i + 1}: Parse error - {ex.Message}";
                    question.IsValid = false;
                }

                questions.Add(question);
            }

            return questions;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PublishExam(int examId)
        {
            try
            {
                var success = _examService.PublishExam(examId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Exam published successfully and is now available to students";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to publish exam. Please ensure the exam has questions with correct answers.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error publishing exam: {ex.Message}";
            }

            return RedirectToAction("ManageExam", new { examId });
        }

        #endregion

        #region Exam Session Management

        /// <summary>
        /// Display exam session management interface
        /// </summary>
        public ActionResult ManageExamSessions()
        {
            var activeSessions = _context.ExamSessions
                .Include("Exam")
                .Where(es => es.IsActive)
                .OrderByDescending(es => es.CreatedDate)
                .ToList();

            return View(activeSessions);
        }

        /// <summary>
        /// Generate exam session password for Phase 2 authentication
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateExamPassword(int examId, string sessionPassword)
        {
            try
            {
                // Comprehensive validation using ValidationService
                var validationResult = _validationService.ValidateSessionPassword(sessionPassword);
                
                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.ToList();
                    var warningMessages = validationResult.Warnings.ToList();

                    if (errorMessages.Any())
                    {
                        TempData["ErrorMessage"] = string.Join("<br/>", errorMessages);
                    }

                    if (warningMessages.Any())
                    {
                        TempData["WarningMessage"] = string.Join("<br/>", warningMessages);
                    }

                    return RedirectToAction("ManageExam", new { examId });
                }

                var exam = _context.Exams.Find(examId);
                if (exam == null)
                {
                    TempData["ErrorMessage"] = "Exam not found";
                    return RedirectToAction("ListExams");
                }

                if (!exam.IsPublished)
                {
                    TempData["ErrorMessage"] = "Cannot generate session password for unpublished exam";
                    return RedirectToAction("ManageExam", new { examId });
                }

                // Deactivate any existing active sessions for this exam
                var existingSessions = _context.ExamSessions
                    .Where(es => es.ExamId == examId && es.IsActive)
                    .ToList();

                foreach (var session in existingSessions)
                {
                    session.IsActive = false;
                }

                // Create new exam session
                var examSession = new ExamSession
                {
                    ExamId = examId,
                    SessionPassword = BCrypt.Net.BCrypt.HashPassword(sessionPassword),
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddHours(24) // Session expires in 24 hours
                };

                _context.ExamSessions.Add(examSession);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"Exam session password generated successfully. Session expires in 24 hours.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating session password: {ex.Message}";
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"GenerateExamPassword Error: {ex.Message}");
            }

            return RedirectToAction("ManageExam", new { examId });
        }

        /// <summary>
        /// Deactivate exam session
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeactivateExamSession(int examSessionId)
        {
            try
            {
                var examSession = _context.ExamSessions.Find(examSessionId);
                if (examSession != null)
                {
                    examSession.IsActive = false;
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Exam session deactivated successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Exam session not found";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deactivating session: {ex.Message}";
            }

            return RedirectToAction("ManageExamSessions");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Process CSV import file and create student records
        /// </summary>
        private List<StudentImportResult> ProcessImportFile(IFormFile file, string formBatchYear, bool skipHeaderRow)
        {
            var results = new List<StudentImportResult>();
            
            using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            {
                var lineNumber = 0;
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    // Skip header row if specified
                    if (lineNumber == 1 && skipHeaderRow)
                        continue;

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Pass formBatchYear as a fallback, but CSV BatchYear takes precedence
                    var result = ProcessImportLine(line, formBatchYear, lineNumber);
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Process a single line from the import file
        /// Expected format: StudentName,IdNumber,Gender,Section,UniversityEmail (5 columns exactly)
        /// BatchYear comes from form input, not CSV
        /// </summary>
        private StudentImportResult ProcessImportLine(string line, string batchYear, int lineNumber)
        {
            var result = new StudentImportResult
            {
                BatchYear = batchYear
            };

            try
            {
                var parts = line.Split(',');
                
                // EXPECT EXACTLY 5 COLUMNS
                if (parts.Length != 5)
                {
                    result.ErrorMessage = $"Line {lineNumber}: Invalid CSV structure. Expected exactly 5 columns but found {parts.Length}.";
                    return result;
                }

                var studentName = parts[0].Trim().Trim('"');  // First column is StudentName
                var idNumber = parts[1].Trim().Trim('"');     // keeps leading zero
                var gender = parts[2].Trim().Trim('"');
                var section = parts[3].Trim().Trim('"');
                var email = parts[4].Trim().Trim('"');

                // ALWAYS set the display fields first, regardless of validation outcome
                result.StudentName = studentName;
                result.IdNumber = idNumber;
                result.Gender = gender;
                result.Section = section;
                result.UniversityEmail = email;

                // Validation
                if (string.IsNullOrWhiteSpace(studentName))
                {
                    result.ErrorMessage = $"Line {lineNumber}: Student Name is required";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(idNumber))
                {
                    result.ErrorMessage = $"Line {lineNumber}: IdNumber is required";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.ErrorMessage = $"Line {lineNumber}: University Email is required";
                    return result;
                }

                // Create student for validation ONLY
                var student = new Student
                {
                    IdNumber = idNumber,
                    UniversityEmail = email,
                    FirstName = ExtractFirstName(studentName),
                    LastName = ExtractLastName(studentName),
                    Email = email,
                    BatchYear = batchYear
                };

                var validationResult = _validationService.ValidateStudentData(student);
                if (!validationResult.IsValid)
                {
                    result.ErrorMessage = string.Join("; ", validationResult.Errors);
                    return result;
                }

                // Create record
                var importModel = new StudentImportModel
                {
                    StudentName = studentName,
                    IdNumber = idNumber,
                    Gender = gender,
                    Section = section,
                    UniversityEmail = email,
                    BatchYear = batchYear
                };

                var created = CreateStudentRecord(importModel);
                if (created.HasValue)
                {
                    result.IsSuccess = true;
                    result.CreatedUserId = created.Value.userId;
                    result.CreatedStudentId = created.Value.studentId;
                }
                else
                {
                    result.ErrorMessage = $"Line {lineNumber}: Failed to create student record";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Line {lineNumber}: {ex.Message}";
            }

            return result;
        }



        /// <summary>
        /// Extract first name from full name
        /// Name Parsing Rule: FirstName = first word
        /// </summary>
        private string ExtractFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "";
            
            var parts = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            return parts[0];
        }

        /// <summary>
        /// Extract last name from full name
        /// Name Parsing Rule: LastName = second word, if only one word exists, use it for both fields
        /// </summary>
        private string ExtractLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "";
            
            var parts = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
                return parts[1];
            
            // fallback for single-name students - use same name for both fields
            return parts[0];
        }

        /// <summary>
        /// Create a new student record with associated user account
        /// </summary>
        private (int userId, int studentId)? CreateStudentRecord(StudentImportModel model)
        {
            try
            {
                // Get Student role
                var studentRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Student");
                if (studentRole == null)
                {
                    // Create Student role if it doesn't exist
                    studentRole = new Role { RoleName = "Student" };
                    _context.Roles.Add(studentRole);
                    _context.SaveChanges();
                }

                // Create User account
                var user = new User
                {
                    Username = model.UniversityEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("temporary"), // Temporary password
                    RoleId = studentRole.RoleId,
                    LoginPhaseCompleted = false,
                    MustChangePassword = true,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // Create Student record with all fields
                var student = new Student
                {
                    UserId = user.UserId,
                    IdNumber = model.IdNumber,
                    UniversityEmail = model.UniversityEmail,
                    FirstName = ExtractFirstName(model.StudentName),
                    LastName = ExtractLastName(model.StudentName),
                    Email = model.UniversityEmail,
                    BatchYear = model.BatchYear,
                    CreatedDate = DateTime.Now
                };

                _context.Students.Add(student);
                _context.SaveChanges();

                return (user.UserId, student.StudentId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Results Management

        /// <summary>
        /// Shows exam statistics and results for coordinators
        /// </summary>
        /// <param name="examId">ID of the exam to view results for</param>
        /// <returns>Exam results view</returns>
        public ActionResult ExamResults(int examId)
        {
            try
            {
                var exam = _context.Exams.Find(examId);
                if (exam == null)
                {
                    TempData["ErrorMessage"] = "Exam not found.";
                    return RedirectToAction("ListExams");
                }

                // Get all student exams for this exam
                var studentExams = _context.StudentExams
                    .Include("Student")
                    .Where(se => se.ExamId == examId && se.IsSubmitted)
                    .ToList();

                // Calculate statistics
                var totalSubmissions = studentExams.Count;
                var averageScore = totalSubmissions > 0 ? studentExams.Where(se => se.Percentage.HasValue).Average(se => se.Percentage ?? 0) : 0;
                var highestScore = totalSubmissions > 0 ? studentExams.Where(se => se.Percentage.HasValue).Max(se => se.Percentage ?? 0) : 0;
                var lowestScore = totalSubmissions > 0 ? studentExams.Where(se => se.Percentage.HasValue).Min(se => se.Percentage ?? 0) : 0;

                // Grade distribution
                var gradeDistribution = new Dictionary<string, int>
                {
                    ["A (90-100%)"] = studentExams.Count(se => se.Percentage >= 90),
                    ["B (80-89%)"] = studentExams.Count(se => se.Percentage >= 80 && se.Percentage < 90),
                    ["C (70-79%)"] = studentExams.Count(se => se.Percentage >= 70 && se.Percentage < 80),
                    ["D (60-69%)"] = studentExams.Count(se => se.Percentage >= 60 && se.Percentage < 70),
                    ["F (Below 60%)"] = studentExams.Count(se => se.Percentage < 60)
                };

                ViewBag.Exam = exam;
                ViewBag.StudentExams = studentExams.OrderByDescending(se => se.Percentage).ToList();
                ViewBag.TotalSubmissions = totalSubmissions;
                ViewBag.AverageScore = Math.Round(averageScore, 2);
                ViewBag.HighestScore = Math.Round(highestScore, 2);
                ViewBag.LowestScore = Math.Round(lowestScore, 2);
                ViewBag.GradeDistribution = gradeDistribution;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading exam results.";
                System.Diagnostics.Debug.WriteLine($"ExamResults Error: {ex.Message}");
                return RedirectToAction("ListExams");
            }
        }

        /// <summary>
        /// Shows detailed results for a specific student exam
        /// </summary>
        /// <param name="studentExamId">ID of the student exam</param>
        /// <returns>Student exam details view</returns>
        public ActionResult StudentExamDetails(int studentExamId)
        {
            try
            {
                var studentExam = _context.StudentExams
                    .Include("Student")
                    .Include("Exam")
                    .FirstOrDefault(se => se.StudentExamId == studentExamId);

                if (studentExam == null)
                {
                    TempData["ErrorMessage"] = "Student exam not found.";
                    return RedirectToAction("ListExams");
                }

                // Get all questions and answers
                var questions = _context.Questions
                    .Where(q => q.ExamId == studentExam.ExamId)
                    .OrderBy(q => q.QuestionOrder)
                    .ToList();

                var studentAnswers = _context.StudentAnswers
                    .Where(sa => sa.StudentExamId == studentExamId)
                    .ToList();

                var questionResults = questions.Select(q => new
                {
                    Question = q,
                    StudentAnswer = studentAnswers.FirstOrDefault(sa => sa.QuestionId == q.QuestionId),
                    Choices = _context.Choices.Where(c => c.QuestionId == q.QuestionId).OrderBy(c => c.ChoiceOrder).ToList(),
                    IsCorrect = studentAnswers.Any(sa => sa.QuestionId == q.QuestionId && 
                                                        sa.ChoiceId.HasValue &&
                                                        _context.Choices.Any(c => c.ChoiceId == sa.ChoiceId.Value && c.IsCorrect))
                }).ToList();

                ViewBag.StudentExam = studentExam;
                ViewBag.QuestionResults = questionResults;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading student exam details.";
                System.Diagnostics.Debug.WriteLine($"StudentExamDetails Error: {ex.Message}");
                return RedirectToAction("ListExams");
            }
        }

        #endregion

        #region Database Optimization Management

        /// <summary>
        /// Shows database optimization status and performance metrics
        /// </summary>
        /// <returns>Database optimization view</returns>
        public ActionResult DatabaseOptimization()
        {
            try
            {
                var metrics = _optimizationService.GetPerformanceMetrics();
                var optimizationsApplied = _optimizationService.AreOptimizationsApplied();

                ViewBag.Metrics = metrics;
                ViewBag.OptimizationsApplied = optimizationsApplied;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while loading database optimization status.";
                System.Diagnostics.Debug.WriteLine($"DatabaseOptimization Error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Applies database optimizations including indexes and statistics updates
        /// </summary>
        /// <returns>Redirect to database optimization view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyOptimizations()
        {
            try
            {
                _optimizationService.ApplyDatabaseOptimizations();
                TempData["SuccessMessage"] = "Database optimizations applied successfully. Performance improvements should be noticeable immediately.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error applying database optimizations: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"ApplyOptimizations Error: {ex.Message}");
            }

            return RedirectToAction("DatabaseOptimization");
        }

        /// <summary>
        /// Updates database statistics for optimal query execution plans
        /// </summary>
        /// <returns>Redirect to database optimization view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatistics()
        {
            try
            {
                _optimizationService.UpdateDatabaseStatistics();
                TempData["SuccessMessage"] = "Database statistics updated successfully. Query performance should be optimized.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating database statistics: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"UpdateStatistics Error: {ex.Message}");
            }

            return RedirectToAction("DatabaseOptimization");
        }

        /// <summary>
        /// Creates authentication-specific indexes for login performance
        /// </summary>
        /// <returns>Redirect to database optimization view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OptimizeAuthentication()
        {
            try
            {
                _optimizationService.CreateAuthenticationIndexes();
                TempData["SuccessMessage"] = "Authentication indexes created successfully. Login performance should be significantly improved.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error optimizing authentication: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"OptimizeAuthentication Error: {ex.Message}");
            }

            return RedirectToAction("DatabaseOptimization");
        }

        /// <summary>
        /// Creates exam-specific indexes for exam access and navigation performance
        /// </summary>
        /// <returns>Redirect to database optimization view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OptimizeExamAccess()
        {
            try
            {
                _optimizationService.CreateExamIndexes();
                TempData["SuccessMessage"] = "Exam access indexes created successfully. Exam loading and navigation should be much faster.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error optimizing exam access: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"OptimizeExamAccess Error: {ex.Message}");
            }

            return RedirectToAction("DatabaseOptimization");
        }

        /// <summary>
        /// Creates grading-specific indexes for score calculation performance
        /// </summary>
        /// <returns>Redirect to database optimization view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OptimizeGrading()
        {
            try
            {
                _optimizationService.CreateGradingIndexes();
                TempData["SuccessMessage"] = "Grading indexes created successfully. Automatic grading should be significantly faster.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error optimizing grading: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"OptimizeGrading Error: {ex.Message}");
            }

            return RedirectToAction("DatabaseOptimization");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}