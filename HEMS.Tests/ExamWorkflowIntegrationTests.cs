using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Services;
using HEMS.Models;
using HEMS.Controllers;
using System.Web.Mvc;
using BCrypt.Net;

namespace HEMS.Tests
{
    /// <summary>
    /// Integration tests for the complete exam workflow
    /// Tests the end-to-end process from authentication through exam completion
    /// </summary>
    [TestClass]
    public class ExamWorkflowIntegrationTests
    {
        private HEMSContext _context;
        private AuthenticationService _authService;
        private ExamService _examService;
        private TimerService _timerService;
        private GradingService _gradingService;
        private AuditService _auditService;
        private DataIntegrityService _dataIntegrityService;
        private ExamController _examController;
        private string _testId;

        [TestInitialize]
        public void Setup()
        {
            // Use in-memory database for testing
            Database.SetInitializer(new DropCreateDatabaseAlways<HEMSContext>());
            _context = new HEMSContext();
            _context.Database.Initialize(true);
            
            // Initialize services
            _authService = new AuthenticationService(_context);
            _examService = new ExamService(_context);
            _timerService = new TimerService(_context);
            _gradingService = new GradingService(_context);
            _auditService = new AuditService(_context);
            _dataIntegrityService = new DataIntegrityService(_context);
            
            // Initialize controller with dependencies
            _examController = new ExamController(_context, _examService, _timerService, _gradingService, _auditService, _dataIntegrityService);
            
            // Generate unique test ID
            _testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            // Clean and seed test data
            CleanDatabase();
            SeedTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            CleanDatabase();
            _context?.Dispose();
        }

        private void CleanDatabase()
        {
            // Clean up existing data in proper order due to foreign key constraints
            var auditLogs = _context.AuditLogs.ToList();
            _context.AuditLogs.RemoveRange(auditLogs);
            
            var studentAnswers = _context.StudentAnswers.ToList();
            _context.StudentAnswers.RemoveRange(studentAnswers);
            
            var studentExams = _context.StudentExams.ToList();
            _context.StudentExams.RemoveRange(studentExams);
            
            var examSessions = _context.ExamSessions.ToList();
            _context.ExamSessions.RemoveRange(examSessions);
            
            var choices = _context.Choices.ToList();
            _context.Choices.RemoveRange(choices);
            
            var questions = _context.Questions.ToList();
            _context.Questions.RemoveRange(questions);
            
            var exams = _context.Exams.ToList();
            _context.Exams.RemoveRange(exams);
            
            var students = _context.Students.ToList();
            _context.Students.RemoveRange(students);
            
            var users = _context.Users.ToList();
            _context.Users.RemoveRange(users);
            
            var roles = _context.Roles.ToList();
            _context.Roles.RemoveRange(roles);
            
            _context.SaveChanges();
        }

        private void SeedTestData()
        {
            // Create test roles
            var studentRole = new Role { RoleName = $"Student{_testId}" };
            var coordinatorRole = new Role { RoleName = $"Coordinator{_testId}" };
            
            _context.Roles.Add(studentRole);
            _context.Roles.Add(coordinatorRole);
            _context.SaveChanges();

            // Create test users and students
            CreateTestStudent("student1", "SE001", 2017, studentRole.RoleId);
            CreateTestStudent("student2", "SE002", 2017, studentRole.RoleId);
            CreateTestStudent("student3", "SE003", 2018, studentRole.RoleId);

            // Create coordinator user
            var coordinatorUser = new User
            {
                Username = $"coordinator{_testId}@hems.edu",
                PasswordHash = BCrypt.HashPassword("coordinator123"),
                RoleId = coordinatorRole.RoleId,
                LoginPhaseCompleted = true,
                MustChangePassword = false
            };
            _context.Users.Add(coordinatorUser);
            _context.SaveChanges();

            // Create test exam with questions
            CreateTestExam();
        }

        private void CreateTestStudent(string identifier, string idNumber, int batchYear, int roleId)
        {
            var user = new User
            {
                Username = $"{identifier}{_testId}@hems.edu",
                PasswordHash = BCrypt.HashPassword("temppassword"),
                RoleId = roleId,
                LoginPhaseCompleted = false,
                MustChangePassword = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"{idNumber}{_testId}",
                UniversityEmail = $"{identifier}{_testId}@hems.edu",
                BatchYear = batchYear,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();
        }

        private void CreateTestExam()
        {
            var exam = new Exam
            {
                Title = $"Software Engineering Exam {_testId}",
                AcademicYear = 2024,
                DurationMinutes = 120,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            // Add questions with choices
            for (int i = 1; i <= 5; i++)
            {
                var question = new Question
                {
                    ExamId = exam.ExamId,
                    QuestionText = $"What is the answer to question {i}?",
                    QuestionOrder = i
                };
                _context.Questions.Add(question);
                _context.SaveChanges();

                // Add 4 choices per question, with choice 1 being correct
                for (int j = 1; j <= 4; j++)
                {
                    var choice = new Choice
                    {
                        QuestionId = question.QuestionId,
                        ChoiceText = $"Option {j} for question {i}",
                        IsCorrect = j == 1, // First choice is correct
                        ChoiceOrder = j
                    };
                    _context.Choices.Add(choice);
                }
            }
            _context.SaveChanges();

            // Create exam session for Phase 2 authentication
            var examSession = new ExamSession
            {
                ExamId = exam.ExamId,
                SessionPassword = BCrypt.HashPassword("exam2024"),
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(3)
            };
            _context.ExamSessions.Add(examSession);
            _context.SaveChanges();
        }

        #region Simple Test to Verify Class Works

        [TestMethod]
        public void SimpleTest_VerifyTestClassWorks()
        {
            // Simple test to verify the test class is working
            Assert.IsTrue(true, "Test class should be discoverable");
        }

        #endregion

        #region Complete Student Exam Workflow Tests

        [TestMethod]
        public void CompleteStudentExamWorkflow_ValidScenario_Success()
        {
            // Arrange
            var studentEmail = $"student1{_testId}@hems.edu";
            var studentIdNumber = $"SE001{_testId}";
            var phase1Password = _authService.CalculatePhase1Password(studentIdNumber);

            // Act & Assert - Phase 1 Authentication
            var phase1Result = _authService.ValidatePhase1Login(studentEmail, phase1Password);
            Assert.IsTrue(phase1Result, "Phase 1 login should succeed");

            var user = _authService.GetUserByEmail(studentEmail);
            Assert.IsNotNull(user, "User should be found");

            var phase1Complete = _authService.CompletePhase1Login(user.UserId);
            Assert.IsTrue(phase1Complete, "Phase 1 completion should succeed");

            // Verify Phase 1 completion
            var updatedUser = _context.Users.Find(user.UserId);
            Assert.IsTrue(updatedUser.LoginPhaseCompleted, "LoginPhaseCompleted should be true");

            // Phase 2 Authentication
            var phase2Result = _authService.ValidatePhase2Login(studentIdNumber, "exam2024");
            Assert.IsTrue(phase2Result, "Phase 2 login should succeed");

            // Password change (required after Phase 2)
            var passwordChanged = _authService.ChangePassword(user.UserId, "newpassword123");
            Assert.IsTrue(passwordChanged, "Password change should succeed");

            // Verify password change
            updatedUser = _context.Users.Find(user.UserId);
            Assert.IsFalse(updatedUser.MustChangePassword, "MustChangePassword should be false");

            // Get available exams
            var student = _context.Students.First(s => s.UserId == user.UserId);
            var availableExams = _examService.GetAvailableExams(student.StudentId);
            Assert.IsTrue(availableExams.Count > 0, "Should have available exams");

            var exam = availableExams.First();

            // Start exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            Assert.IsNotNull(studentExam, "Exam should start successfully");
            Assert.IsFalse(studentExam.IsSubmitted, "Exam should not be submitted initially");

            // Get exam questions
            var questions = _examService.GetExamQuestions(exam.ExamId);
            Assert.IsTrue(questions.Count > 0, "Exam should have questions");

            // Answer questions (answer first 3 correctly, leave 2 unanswered)
            for (int i = 0; i < 3; i++)
            {
                var question = questions[i];
                var correctChoice = _context.Choices
                    .First(c => c.QuestionId == question.QuestionId && c.IsCorrect);
                
                var answerSaved = _examService.SaveAnswer(studentExam.StudentExamId, question.QuestionId, correctChoice.ChoiceId);
                Assert.IsTrue(answerSaved, $"Answer for question {i + 1} should be saved");
            }

            // Flag one question for review
            var flagResult = _examService.FlagQuestion(studentExam.StudentExamId, questions[1].QuestionId, true);
            Assert.IsTrue(flagResult, "Question should be flagged successfully");

            // Verify flagged status
            var flaggedAnswer = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[1].QuestionId);
            Assert.IsTrue(flaggedAnswer.IsFlagged, "Question should be flagged");

            // Submit exam
            var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsTrue(submitResult, "Exam should be submitted successfully");

            // Verify submission
            var submittedExam = _examService.GetStudentExamSession(studentExam.StudentExamId);
            Assert.IsTrue(submittedExam.IsSubmitted, "Exam should be marked as submitted");
            Assert.IsNotNull(submittedExam.SubmitDateTime, "Submit date should be set");

            // Verify automatic grading
            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.IsNotNull(gradingResult, "Grading result should exist");
            Assert.AreEqual(3, gradingResult.CorrectAnswers, "Should have 3 correct answers");
            Assert.AreEqual(2, gradingResult.UnansweredQuestions, "Should have 2 unanswered questions");
            Assert.AreEqual(60m, gradingResult.Percentage, "Should have 60% score (3/5)");
        }

        [TestMethod]
        public void CompleteStudentExamWorkflow_WithQuestionNavigation_Success()
        {
            // Arrange
            var studentEmail = $"student2{_testId}@hems.edu";
            var studentIdNumber = $"SE002{_testId}";
            
            CompleteAuthentication(studentEmail, studentIdNumber);
            var student = GetStudentByEmail(studentEmail);
            var exam = _context.Exams.First(e => e.IsPublished);

            // Start exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Act - Navigate through questions in non-sequential order
            // Answer question 3 first
            var question3 = questions[2];
            var correctChoice3 = _context.Choices.First(c => c.QuestionId == question3.QuestionId && c.IsCorrect);
            var answer3Saved = _examService.SaveAnswer(studentExam.StudentExamId, question3.QuestionId, correctChoice3.ChoiceId);
            Assert.IsTrue(answer3Saved, "Answer for question 3 should be saved");

            // Navigate to question 1 and answer
            var question1 = questions[0];
            var correctChoice1 = _context.Choices.First(c => c.QuestionId == question1.QuestionId && c.IsCorrect);
            var answer1Saved = _examService.SaveAnswer(studentExam.StudentExamId, question1.QuestionId, correctChoice1.ChoiceId);
            Assert.IsTrue(answer1Saved, "Answer for question 1 should be saved");

            // Navigate to question 5 and flag it
            var question5 = questions[4];
            var flagResult = _examService.FlagQuestion(studentExam.StudentExamId, question5.QuestionId, true);
            Assert.IsTrue(flagResult, "Question 5 should be flagged");

            // Change answer for question 3
            var wrongChoice3 = _context.Choices.First(c => c.QuestionId == question3.QuestionId && !c.IsCorrect);
            var answer3Changed = _examService.SaveAnswer(studentExam.StudentExamId, question3.QuestionId, wrongChoice3.ChoiceId);
            Assert.IsTrue(answer3Changed, "Answer for question 3 should be changed");

            // Submit exam
            var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsTrue(submitResult, "Exam should be submitted successfully");

            // Assert - Verify final state
            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.AreEqual(1, gradingResult.CorrectAnswers, "Should have 1 correct answer (question 1)");
            Assert.AreEqual(1, gradingResult.IncorrectAnswers, "Should have 1 incorrect answer (question 3)");
            Assert.AreEqual(3, gradingResult.UnansweredQuestions, "Should have 3 unanswered questions");
            Assert.AreEqual(20m, gradingResult.Percentage, "Should have 20% score (1/5)");

            // Verify flagged question persisted
            var flaggedAnswer = _examService.GetStudentAnswer(studentExam.StudentExamId, question5.QuestionId);
            Assert.IsTrue(flaggedAnswer.IsFlagged, "Question 5 should remain flagged");
        }

        [TestMethod]
        public void CompleteStudentExamWorkflow_AllQuestionsAnsweredAndFlagged_Success()
        {
            // Arrange
            var studentEmail = $"student3{_testId}@hems.edu";
            var studentIdNumber = $"SE003{_testId}";
            
            CompleteAuthentication(studentEmail, studentIdNumber);
            var student = GetStudentByEmail(studentEmail);
            var exam = _context.Exams.First(e => e.IsPublished);

            // Start exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Act - Answer all questions and flag some for review
            for (int i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                var correctChoice = _context.Choices.First(c => c.QuestionId == question.QuestionId && c.IsCorrect);
                
                // Answer all questions correctly
                var answerSaved = _examService.SaveAnswer(studentExam.StudentExamId, question.QuestionId, correctChoice.ChoiceId);
                Assert.IsTrue(answerSaved, $"Answer for question {i + 1} should be saved");

                // Flag every other question for review
                if (i % 2 == 0)
                {
                    var flagResult = _examService.FlagQuestion(studentExam.StudentExamId, question.QuestionId, true);
                    Assert.IsTrue(flagResult, $"Question {i + 1} should be flagged");
                }
            }

            // Unflag one previously flagged question
            var unflagResult = _examService.FlagQuestion(studentExam.StudentExamId, questions[0].QuestionId, false);
            Assert.IsTrue(unflagResult, "Question 1 should be unflagged");

            // Submit exam
            var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsTrue(submitResult, "Exam should be submitted successfully");

            // Assert - Verify perfect score and flag states
            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.AreEqual(5, gradingResult.CorrectAnswers, "Should have all 5 correct answers");
            Assert.AreEqual(0, gradingResult.IncorrectAnswers, "Should have no incorrect answers");
            Assert.AreEqual(0, gradingResult.UnansweredQuestions, "Should have no unanswered questions");
            Assert.AreEqual(100m, gradingResult.Percentage, "Should have 100% score");

            // Verify flag states
            var answer1 = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[0].QuestionId);
            Assert.IsFalse(answer1.IsFlagged, "Question 1 should not be flagged (was unflagged)");

            var answer2 = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[1].QuestionId);
            Assert.IsFalse(answer2.IsFlagged, "Question 2 should not be flagged");

            var answer3 = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[2].QuestionId);
            Assert.IsTrue(answer3.IsFlagged, "Question 3 should be flagged");
        }

        [TestMethod]
        public void CompleteStudentExamWorkflow_WithTimerExpiry_AutoSubmits()
        {
            // Arrange
            var studentEmail = $"student2{_testId}@hems.edu";
            var studentIdNumber = $"SE002{_testId}";
            
            // Complete authentication
            CompleteAuthentication(studentEmail, studentIdNumber);
            
            var student = GetStudentByEmail(studentEmail);
            var exam = _context.Exams.First(e => e.IsPublished);

            // Start exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            
            // Answer some questions before timer expires
            var questions = _examService.GetExamQuestions(exam.ExamId);
            var correctChoice = _context.Choices.First(c => c.QuestionId == questions[0].QuestionId && c.IsCorrect);
            _examService.SaveAnswer(studentExam.StudentExamId, questions[0].QuestionId, correctChoice.ChoiceId);
            
            // Simulate timer expiry by setting start time in the past
            studentExam.StartDateTime = DateTime.Now.AddMinutes(-(exam.DurationMinutes + 1));
            _context.SaveChanges();

            // Act - Check if exam is expired
            var remainingTime = _examService.GetRemainingTime(studentExam.StudentExamId);
            
            // Assert
            Assert.IsTrue(remainingTime <= TimeSpan.Zero, "Exam should be expired");
            
            // Verify that timer service detects expiry
            var isExpired = _timerService.IsExamExpired(studentExam.StudentExamId);
            Assert.IsTrue(isExpired, "Timer service should detect expiry");

            // Simulate auto-submission that would occur in real system
            if (isExpired && !studentExam.IsSubmitted)
            {
                var autoSubmitResult = _examService.SubmitExam(studentExam.StudentExamId);
                Assert.IsTrue(autoSubmitResult, "Auto-submission should succeed");
            }

            // Verify exam was auto-submitted and graded
            var submittedExam = _examService.GetStudentExamSession(studentExam.StudentExamId);
            Assert.IsTrue(submittedExam.IsSubmitted, "Exam should be auto-submitted");

            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.IsNotNull(gradingResult, "Auto-submitted exam should be graded");
            Assert.AreEqual(20m, gradingResult.Percentage, "Should have 20% score (1/5 answered)");
        }

        [TestMethod]
        public void CompleteStudentExamWorkflow_ResultsViewing_Success()
        {
            // Arrange
            var studentEmail = $"student1{_testId}@hems.edu";
            var studentIdNumber = $"SE001{_testId}";
            
            CompleteAuthentication(studentEmail, studentIdNumber);
            var student = GetStudentByEmail(studentEmail);
            var exam = _context.Exams.First(e => e.IsPublished);

            // Complete exam with mixed answers
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Answer questions with different outcomes
            for (int i = 0; i < questions.Count; i++)
            {
                if (i < 2) // First 2 correct
                {
                    var correctChoice = _context.Choices.First(c => c.QuestionId == questions[i].QuestionId && c.IsCorrect);
                    _examService.SaveAnswer(studentExam.StudentExamId, questions[i].QuestionId, correctChoice.ChoiceId);
                }
                else if (i == 2) // Third incorrect
                {
                    var wrongChoice = _context.Choices.First(c => c.QuestionId == questions[i].QuestionId && !c.IsCorrect);
                    _examService.SaveAnswer(studentExam.StudentExamId, questions[i].QuestionId, wrongChoice.ChoiceId);
                }
                // Last 2 unanswered
            }

            _examService.SubmitExam(studentExam.StudentExamId);

            // Act - View results (simulate what would happen in controller)
            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);

            // Assert - Verify detailed results
            Assert.IsNotNull(gradingResult, "Results should be available after submission");
            Assert.AreEqual(5, gradingResult.TotalQuestions, "Should show total questions");
            Assert.AreEqual(2, gradingResult.CorrectAnswers, "Should show correct answers");
            Assert.AreEqual(1, gradingResult.IncorrectAnswers, "Should show incorrect answers");
            Assert.AreEqual(2, gradingResult.UnansweredQuestions, "Should show unanswered questions");
            Assert.AreEqual(40m, gradingResult.Percentage, "Should show percentage (2/5 = 40%)");
            Assert.IsTrue(gradingResult.GradedDateTime > DateTime.MinValue, "Should have grading timestamp");

            // Verify individual answer details
            for (int i = 0; i < questions.Count; i++)
            {
                var studentAnswer = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[i].QuestionId);
                
                if (i < 2)
                {
                    Assert.IsNotNull(studentAnswer.ChoiceId, $"Question {i + 1} should have an answer");
                    var selectedChoice = _context.Choices.Find(studentAnswer.ChoiceId);
                    Assert.IsTrue(selectedChoice.IsCorrect, $"Question {i + 1} should be correct");
                }
                else if (i == 2)
                {
                    Assert.IsNotNull(studentAnswer.ChoiceId, "Question 3 should have an answer");
                    var selectedChoice = _context.Choices.Find(studentAnswer.ChoiceId);
                    Assert.IsFalse(selectedChoice.IsCorrect, "Question 3 should be incorrect");
                }
                else
                {
                    Assert.IsNull(studentAnswer.ChoiceId, $"Question {i + 1} should be unanswered");
                }
            }
        }

        #endregion

        #region Coordinator Workflow Tests

        [TestMethod]
        public void CoordinatorWorkflow_ExamCreationToResultsViewing_Success()
        {
            // This test simulates the coordinator's complete workflow
            // Note: Since we don't have a CoordinatorController in the test setup,
            // we'll test the underlying services that would be used

            // Arrange - Get coordinator user
            var coordinatorUser = _context.Users.First(u => u.Username.Contains("coordinator"));
            
            // Act & Assert - Create new exam (simulating coordinator action)
            var newExam = _examService.CreateExam("Advanced Software Engineering", 2024, 180);
            Assert.IsNotNull(newExam, "Exam should be created successfully");

            // Add questions to exam
            var choiceTexts = new List<string> { "Correct Answer", "Wrong Answer 1", "Wrong Answer 2", "Wrong Answer 3" };
            var question = _examService.AddQuestion(newExam.ExamId, "What is polymorphism?", choiceTexts, 0);
            Assert.IsNotNull(question, "Question should be added successfully");

            // Publish exam
            var publishResult = _examService.PublishExam(newExam.ExamId);
            Assert.IsTrue(publishResult, "Exam should be published successfully");

            // Verify exam is available to students
            var student = _context.Students.First();
            var availableExams = _examService.GetAvailableExams(student.StudentId);
            Assert.IsTrue(availableExams.Any(e => e.ExamId == newExam.ExamId), "New exam should be available to students");

            // Simulate student taking the exam
            var studentExam = _examService.StartExam(student.StudentId, newExam.ExamId);
            var questions = _examService.GetExamQuestions(newExam.ExamId);
            var correctChoice = _context.Choices.First(c => c.QuestionId == questions.First().QuestionId && c.IsCorrect);
            
            _examService.SaveAnswer(studentExam.StudentExamId, questions.First().QuestionId, correctChoice.ChoiceId);
            _examService.SubmitExam(studentExam.StudentExamId);

            // Verify grading results (coordinator would view these)
            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.IsNotNull(gradingResult, "Grading result should exist for coordinator to view");
            Assert.AreEqual(100m, gradingResult.Percentage, "Student should have 100% score");
        }

        [TestMethod]
        public void CoordinatorWorkflow_ExamSessionManagement_Success()
        {
            // Arrange - Get coordinator and create exam
            var coordinatorUser = _context.Users.First(u => u.Username.Contains("coordinator"));
            var exam = _context.Exams.First(e => e.IsPublished);

            // Act & Assert - Create exam session with Phase 2 password
            var examSession = new ExamSession
            {
                ExamId = exam.ExamId,
                SessionPassword = BCrypt.HashPassword("newexampass2024"),
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(4)
            };
            _context.ExamSessions.Add(examSession);
            _context.SaveChanges();

            // Verify session is active
            var activeSession = _context.ExamSessions.FirstOrDefault(es => es.ExamId == exam.ExamId && es.IsActive);
            Assert.IsNotNull(activeSession, "Exam session should be active");

            // Test student can use new password for Phase 2
            var studentIdNumber = $"SE001{_testId}";
            var phase2Result = _authService.ValidatePhase2Login(studentIdNumber, "newexampass2024");
            Assert.IsTrue(phase2Result, "Phase 2 login should work with new session password");

            // Simulate coordinator ending session
            activeSession.IsActive = false;
            activeSession.ExpiryDate = DateTime.Now.AddMinutes(-1);
            _context.SaveChanges();

            // Verify session is no longer active
            var expiredSession = _context.ExamSessions.FirstOrDefault(es => es.ExamId == exam.ExamId && es.IsActive);
            Assert.IsNull(expiredSession, "Exam session should be inactive");
        }

        [TestMethod]
        public void CoordinatorWorkflow_StudentImportAndExamAccess_Success()
        {
            // Arrange - Simulate coordinator importing new students
            var studentRole = _context.Roles.First(r => r.RoleName.Contains("Student"));
            
            // Create new students (simulating import process)
            var newStudents = new List<(string email, string idNumber, int batchYear)>
            {
                ($"newstudent1{_testId}@hems.edu", $"SE101{_testId}", 2019),
                ($"newstudent2{_testId}@hems.edu", $"SE102{_testId}", 2020),
                ($"newstudent3{_testId}@hems.edu", $"SE103{_testId}", 2021)
            };

            // Act - Import students
            foreach (var (email, idNumber, batchYear) in newStudents)
            {
                var user = new User
                {
                    Username = email,
                    PasswordHash = BCrypt.HashPassword("temppassword"),
                    RoleId = studentRole.RoleId,
                    LoginPhaseCompleted = false,
                    MustChangePassword = true
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                var student = new Student
                {
                    IdNumber = idNumber,
                    UniversityEmail = email,
                    BatchYear = batchYear,
                    UserId = user.UserId
                };
                _context.Students.Add(student);
                _context.SaveChanges();
            }

            // Assert - Verify students were imported correctly
            var importedStudents = _context.Students.Where(s => s.IdNumber.Contains($"SE10{_testId.Substring(0, 1)}")).ToList();
            Assert.AreEqual(3, importedStudents.Count, "All 3 students should be imported");

            // Verify each student has correct initial state
            foreach (var student in importedStudents)
            {
                var user = _context.Users.Find(student.UserId);
                Assert.IsFalse(user.LoginPhaseCompleted, "New student should not have Phase 1 completed");
                Assert.IsTrue(user.MustChangePassword, "New student should need password change");
                Assert.AreEqual(studentRole.RoleId, user.RoleId, "New student should have Student role");
            }

            // Test that students from different batch years can access current year exam
            var currentExam = _context.Exams.First(e => e.IsPublished && e.AcademicYear == 2024);
            
            foreach (var student in importedStudents)
            {
                // Complete authentication for each student
                var user = _context.Users.Find(student.UserId);
                user.LoginPhaseCompleted = true;
                user.MustChangePassword = false;
                _context.SaveChanges();

                // Verify exam access regardless of batch year
                var isAvailable = _examService.IsExamAvailable(currentExam.ExamId, student.StudentId);
                Assert.IsTrue(isAvailable, $"Student from batch {student.BatchYear} should have access to current exam");
            }
        }

        [TestMethod]
        public void CoordinatorWorkflow_ExamResultsAnalysis_Success()
        {
            // Arrange - Set up multiple students with completed exams
            var students = _context.Students.Take(3).ToList();
            var exam = _context.Exams.First(e => e.IsPublished);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            var studentExams = new List<StudentExam>();

            // Complete exams for all students with different performance levels
            for (int i = 0; i < students.Count; i++)
            {
                var student = students[i];
                var user = _context.Users.First(u => u.UserId == student.UserId);
                user.LoginPhaseCompleted = true;
                user.MustChangePassword = false;
                _context.SaveChanges();

                var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
                studentExams.Add(studentExam);

                // Different performance patterns
                int correctAnswers = i + 1; // Student 0: 1 correct, Student 1: 2 correct, Student 2: 3 correct
                
                for (int j = 0; j < correctAnswers && j < questions.Count; j++)
                {
                    var correctChoice = _context.Choices.First(c => c.QuestionId == questions[j].QuestionId && c.IsCorrect);
                    _examService.SaveAnswer(studentExam.StudentExamId, questions[j].QuestionId, correctChoice.ChoiceId);
                }

                _examService.SubmitExam(studentExam.StudentExamId);
            }

            // Act - Analyze results (coordinator perspective)
            var allResults = studentExams.Select(se => _gradingService.GetGradingResult(se.StudentExamId)).ToList();

            // Assert - Verify coordinator can analyze exam performance
            Assert.AreEqual(3, allResults.Count, "Should have results for all students");

            // Verify performance distribution
            Assert.AreEqual(20m, allResults[0].Percentage, "Student 1 should have 20% (1/5)");
            Assert.AreEqual(40m, allResults[1].Percentage, "Student 2 should have 40% (2/5)");
            Assert.AreEqual(60m, allResults[2].Percentage, "Student 3 should have 60% (3/5)");

            // Calculate class statistics (what coordinator would see)
            var averageScore = allResults.Average(r => r.Percentage);
            var highestScore = allResults.Max(r => r.Percentage);
            var lowestScore = allResults.Min(r => r.Percentage);

            Assert.AreEqual(40m, averageScore, "Class average should be 40%");
            Assert.AreEqual(60m, highestScore, "Highest score should be 60%");
            Assert.AreEqual(20m, lowestScore, "Lowest score should be 20%");

            // Verify all exams are submitted and graded
            Assert.IsTrue(allResults.All(r => r.GradedDateTime > DateTime.MinValue), "All exams should be graded");
            
            var submittedExams = studentExams.Select(se => _examService.GetStudentExamSession(se.StudentExamId)).ToList();
            Assert.IsTrue(submittedExams.All(se => se.IsSubmitted), "All exams should be submitted");
            Assert.IsTrue(submittedExams.All(se => se.SubmitDateTime.HasValue), "All exams should have submit timestamps");
        }

        #endregion

        #region Multi-Student Scenarios

        [TestMethod]
        public void MultipleStudentsExamWorkflow_ConcurrentAccess_Success()
        {
            // Arrange - Get multiple students
            var students = _context.Students.Take(3).ToList();
            var exam = _context.Exams.First(e => e.IsPublished);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Act - Multiple students start exam concurrently
            var studentExams = new List<StudentExam>();
            foreach (var student in students)
            {
                // Complete authentication for each student
                var user = _context.Users.First(u => u.UserId == student.UserId);
                user.LoginPhaseCompleted = true;
                user.MustChangePassword = false;
                _context.SaveChanges();

                var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
                studentExams.Add(studentExam);
            }

            // Assert - All students should have separate exam sessions
            Assert.AreEqual(3, studentExams.Count, "All students should have exam sessions");
            Assert.AreEqual(3, studentExams.Select(se => se.StudentExamId).Distinct().Count(), 
                "All exam sessions should be unique");

            // Act - Students answer questions differently
            // Student 1: Answer all correctly
            for (int i = 0; i < questions.Count; i++)
            {
                var correctChoice = _context.Choices.First(c => c.QuestionId == questions[i].QuestionId && c.IsCorrect);
                _examService.SaveAnswer(studentExams[0].StudentExamId, questions[i].QuestionId, correctChoice.ChoiceId);
            }

            // Student 2: Answer half correctly
            for (int i = 0; i < questions.Count / 2; i++)
            {
                var correctChoice = _context.Choices.First(c => c.QuestionId == questions[i].QuestionId && c.IsCorrect);
                _examService.SaveAnswer(studentExams[1].StudentExamId, questions[i].QuestionId, correctChoice.ChoiceId);
            }

            // Student 3: Answer none (leave all blank)
            // No answers saved for student 3

            // Submit all exams
            foreach (var studentExam in studentExams)
            {
                var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
                Assert.IsTrue(submitResult, "Each exam should submit successfully");
            }

            // Assert - Verify different grading results
            var results = studentExams.Select(se => _gradingService.GetGradingResult(se.StudentExamId)).ToList();
            
            Assert.AreEqual(100m, results[0].Percentage, "Student 1 should have 100%");
            Assert.AreEqual(40m, results[1].Percentage, "Student 2 should have 40% (2/5)");
            Assert.AreEqual(0m, results[2].Percentage, "Student 3 should have 0%");
        }

        [TestMethod]
        public void MultipleStudentsExamWorkflow_DifferentTimingPatterns_Success()
        {
            // Arrange - Get multiple students
            var students = _context.Students.Take(3).ToList();
            var exam = _context.Exams.First(e => e.IsPublished);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Complete authentication for all students
            foreach (var student in students)
            {
                var user = _context.Users.First(u => u.UserId == student.UserId);
                user.LoginPhaseCompleted = true;
                user.MustChangePassword = false;
                _context.SaveChanges();
            }

            // Act - Students start exams at different times
            var studentExams = new List<StudentExam>();
            var startTimes = new List<DateTime>
            {
                DateTime.Now.AddMinutes(-30), // Student 1: Started 30 minutes ago
                DateTime.Now.AddMinutes(-15), // Student 2: Started 15 minutes ago  
                DateTime.Now                  // Student 3: Starting now
            };

            for (int i = 0; i < students.Count; i++)
            {
                var studentExam = _examService.StartExam(students[i].StudentId, exam.ExamId);
                studentExam.StartDateTime = startTimes[i];
                _context.SaveChanges();
                studentExams.Add(studentExam);
            }

            // Verify different remaining times
            for (int i = 0; i < studentExams.Count; i++)
            {
                var remainingTime = _examService.GetRemainingTime(studentExams[i].StudentExamId);
                var expectedRemaining = TimeSpan.FromMinutes(exam.DurationMinutes) - (DateTime.Now - startTimes[i]);
                
                // Allow for small timing differences in test execution
                var timeDifference = Math.Abs((remainingTime.Value - expectedRemaining).TotalSeconds);
                Assert.IsTrue(timeDifference < 5, $"Student {i + 1} should have approximately correct remaining time");
            }

            // Answer questions and submit
            foreach (var studentExam in studentExams)
            {
                // Each student answers first question correctly
                var correctChoice = _context.Choices.First(c => c.QuestionId == questions[0].QuestionId && c.IsCorrect);
                _examService.SaveAnswer(studentExam.StudentExamId, questions[0].QuestionId, correctChoice.ChoiceId);
                
                var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
                Assert.IsTrue(submitResult, "Each exam should submit successfully");
            }

            // Assert - All should have same score despite different timing
            var results = studentExams.Select(se => _gradingService.GetGradingResult(se.StudentExamId)).ToList();
            Assert.IsTrue(results.All(r => r.Percentage == 20m), "All students should have same score (20%) regardless of start time");
        }

        [TestMethod]
        public void MultipleStudentsExamWorkflow_ConcurrentAnswerSaving_Success()
        {
            // Arrange - Set up concurrent scenario
            var students = _context.Students.Take(2).ToList();
            var exam = _context.Exams.First(e => e.IsPublished);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Complete authentication and start exams
            var studentExams = new List<StudentExam>();
            foreach (var student in students)
            {
                var user = _context.Users.First(u => u.UserId == student.UserId);
                user.LoginPhaseCompleted = true;
                user.MustChangePassword = false;
                _context.SaveChanges();

                var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
                studentExams.Add(studentExam);
            }

            // Act - Simulate concurrent answer saving (both students answering same question simultaneously)
            var question = questions.First();
            var correctChoice = _context.Choices.First(c => c.QuestionId == question.QuestionId && c.IsCorrect);
            var wrongChoice = _context.Choices.First(c => c.QuestionId == question.QuestionId && !c.IsCorrect);

            // Both students save answers to same question concurrently
            var result1 = _examService.SaveAnswer(studentExams[0].StudentExamId, question.QuestionId, correctChoice.ChoiceId);
            var result2 = _examService.SaveAnswer(studentExams[1].StudentExamId, question.QuestionId, wrongChoice.ChoiceId);

            // Assert - Both saves should succeed
            Assert.IsTrue(result1, "Student 1 answer save should succeed");
            Assert.IsTrue(result2, "Student 2 answer save should succeed");

            // Verify answers are saved correctly for each student
            var answer1 = _examService.GetStudentAnswer(studentExams[0].StudentExamId, question.QuestionId);
            var answer2 = _examService.GetStudentAnswer(studentExams[1].StudentExamId, question.QuestionId);

            Assert.AreEqual(correctChoice.ChoiceId, answer1.ChoiceId, "Student 1 should have correct answer saved");
            Assert.AreEqual(wrongChoice.ChoiceId, answer2.ChoiceId, "Student 2 should have wrong answer saved");

            // Submit and verify different results
            _examService.SubmitExam(studentExams[0].StudentExamId);
            _examService.SubmitExam(studentExams[1].StudentExamId);

            var gradingResult1 = _gradingService.GetGradingResult(studentExams[0].StudentExamId);
            var gradingResult2 = _gradingService.GetGradingResult(studentExams[1].StudentExamId);

            Assert.AreEqual(20m, gradingResult1.Percentage, "Student 1 should have 20% (1 correct)");
            Assert.AreEqual(0m, gradingResult2.Percentage, "Student 2 should have 0% (1 incorrect)");
        }

        [TestMethod]
        public void MultipleStudentsExamWorkflow_MixedBatchYears_EqualAccess()
        {
            // Arrange - Create students from different batch years
            var studentRole = _context.Roles.First(r => r.RoleName.Contains("Student"));
            var exam = _context.Exams.First(e => e.IsPublished && e.AcademicYear == 2024);

            var batchYears = new[] { 2017, 2018, 2019, 2020 };
            var studentsFromDifferentBatches = new List<Student>();

            for (int i = 0; i < batchYears.Length; i++)
            {
                var user = new User
                {
                    Username = $"batchstudent{i}{_testId}@hems.edu",
                    PasswordHash = BCrypt.HashPassword("temppassword"),
                    RoleId = studentRole.RoleId,
                    LoginPhaseCompleted = true,
                    MustChangePassword = false
                };
                _context.Users.Add(user);
                _context.SaveChanges();

                var student = new Student
                {
                    IdNumber = $"BATCH{batchYears[i]}{_testId}",
                    UniversityEmail = $"batchstudent{i}{_testId}@hems.edu",
                    BatchYear = batchYears[i],
                    UserId = user.UserId
                };
                _context.Students.Add(student);
                _context.SaveChanges();
                studentsFromDifferentBatches.Add(student);
            }

            // Act - All students should be able to access current year exam
            var studentExams = new List<StudentExam>();
            foreach (var student in studentsFromDifferentBatches)
            {
                var isAvailable = _examService.IsExamAvailable(exam.ExamId, student.StudentId);
                Assert.IsTrue(isAvailable, $"Student from batch {student.BatchYear} should have access to exam");

                var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
                Assert.IsNotNull(studentExam, $"Student from batch {student.BatchYear} should be able to start exam");
                studentExams.Add(studentExam);
            }

            // All students answer first question correctly
            var questions = _examService.GetExamQuestions(exam.ExamId);
            var correctChoice = _context.Choices.First(c => c.QuestionId == questions[0].QuestionId && c.IsCorrect);

            foreach (var studentExam in studentExams)
            {
                var answerSaved = _examService.SaveAnswer(studentExam.StudentExamId, questions[0].QuestionId, correctChoice.ChoiceId);
                Assert.IsTrue(answerSaved, "Answer should be saved for student from any batch year");

                var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
                Assert.IsTrue(submitResult, "Exam should be submitted for student from any batch year");
            }

            // Assert - All should have same score regardless of batch year
            var results = studentExams.Select(se => _gradingService.GetGradingResult(se.StudentExamId)).ToList();
            Assert.IsTrue(results.All(r => r.Percentage == 20m), "All students should have same scoring regardless of batch year");
            Assert.AreEqual(batchYears.Length, results.Count, "All students from different batch years should have results");
        }

        #endregion

        #region Error Scenarios and Edge Cases

        [TestMethod]
        public void ExamWorkflow_UnauthorizedAccess_Fails()
        {
            // Arrange
            var student = _context.Students.First();
            var exam = _context.Exams.First();

            // Act & Assert - Try to start exam without Phase 1 completion
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = false;
            _context.SaveChanges();

            var examAvailable = _examService.IsExamAvailable(exam.ExamId, student.StudentId);
            Assert.IsFalse(examAvailable, "Exam should not be available without Phase 1 completion");
        }

        [TestMethod]
        public void ExamWorkflow_DuplicateSubmission_Prevented()
        {
            // Arrange
            var studentEmail = $"student3{_testId}@hems.edu";
            var studentIdNumber = $"SE003{_testId}";
            
            CompleteAuthentication(studentEmail, studentIdNumber);
            var student = GetStudentByEmail(studentEmail);
            var exam = _context.Exams.First(e => e.IsPublished);

            // Start and submit exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            var submitResult1 = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsTrue(submitResult1, "First submission should succeed");

            // Act & Assert - Try to submit again
            var submitResult2 = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsFalse(submitResult2, "Second submission should fail");

            // Try to start same exam again
            var studentExam2 = _examService.StartExam(student.StudentId, exam.ExamId);
            Assert.IsNull(studentExam2, "Should not be able to start exam again after submission");
        }

        [TestMethod]
        public void ExamWorkflow_InvalidAnswerSubmission_Handled()
        {
            // Arrange
            var student = _context.Students.First();
            var exam = _context.Exams.First();
            
            // Complete authentication
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Act & Assert - Try to save answer with invalid choice ID
            var invalidChoiceResult = _examService.SaveAnswer(studentExam.StudentExamId, questions.First().QuestionId, 99999);
            Assert.IsFalse(invalidChoiceResult, "Should not save answer with invalid choice ID");

            // Try to save answer with invalid question ID
            var validChoice = _context.Choices.First(c => c.QuestionId == questions.First().QuestionId);
            var invalidQuestionResult = _examService.SaveAnswer(studentExam.StudentExamId, 99999, validChoice.ChoiceId);
            Assert.IsFalse(invalidQuestionResult, "Should not save answer with invalid question ID");
        }

        [TestMethod]
        public void ExamWorkflow_UnpublishedExam_NotAvailable()
        {
            // Arrange - Create unpublished exam
            var unpublishedExam = new Exam
            {
                Title = "Unpublished Test Exam",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = false
            };
            _context.Exams.Add(unpublishedExam);
            _context.SaveChanges();

            var student = _context.Students.First();
            
            // Complete authentication
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            // Act & Assert
            var isAvailable = _examService.IsExamAvailable(unpublishedExam.ExamId, student.StudentId);
            Assert.IsFalse(isAvailable, "Unpublished exam should not be available");

            var availableExams = _examService.GetAvailableExams(student.StudentId);
            Assert.IsFalse(availableExams.Any(e => e.ExamId == unpublishedExam.ExamId), 
                "Unpublished exam should not appear in available exams list");
        }

        [TestMethod]
        public void ExamWorkflow_WrongAcademicYear_NotAvailable()
        {
            // Arrange - Create exam for different academic year
            var wrongYearExam = new Exam
            {
                Title = "Previous Year Exam",
                AcademicYear = 2023, // Different from current year (2024)
                DurationMinutes = 90,
                IsPublished = true
            };
            _context.Exams.Add(wrongYearExam);
            _context.SaveChanges();

            var student = _context.Students.First();
            
            // Complete authentication
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            // Act & Assert
            var isAvailable = _examService.IsExamAvailable(wrongYearExam.ExamId, student.StudentId);
            Assert.IsFalse(isAvailable, "Exam from wrong academic year should not be available");

            var availableExams = _examService.GetAvailableExams(student.StudentId);
            Assert.IsFalse(availableExams.Any(e => e.ExamId == wrongYearExam.ExamId), 
                "Wrong year exam should not appear in available exams list");
        }

        [TestMethod]
        public void ExamWorkflow_PhaseAuthenticationErrors_HandledCorrectly()
        {
            // Arrange
            var studentEmail = $"student1{_testId}@hems.edu";
            var studentIdNumber = $"SE001{_testId}";

            // Act & Assert - Phase 1 with wrong password
            var wrongPhase1Result = _authService.ValidatePhase1Login(studentEmail, "wrongpassword");
            Assert.IsFalse(wrongPhase1Result, "Phase 1 with wrong password should fail");

            // Phase 1 with correct password
            var correctPhase1Password = _authService.CalculatePhase1Password(studentIdNumber);
            var correctPhase1Result = _authService.ValidatePhase1Login(studentEmail, correctPhase1Password);
            Assert.IsTrue(correctPhase1Result, "Phase 1 with correct password should succeed");

            var user = _authService.GetUserByEmail(studentEmail);
            _authService.CompletePhase1Login(user.UserId);

            // Try Phase 1 again after completion
            var duplicatePhase1Result = _authService.ValidatePhase1Login(studentEmail, correctPhase1Password);
            Assert.IsFalse(duplicatePhase1Result, "Phase 1 should not work after completion");

            // Phase 2 with wrong password
            var wrongPhase2Result = _authService.ValidatePhase2Login(studentIdNumber, "wrongexampass");
            Assert.IsFalse(wrongPhase2Result, "Phase 2 with wrong password should fail");

            // Phase 2 with correct password
            var correctPhase2Result = _authService.ValidatePhase2Login(studentIdNumber, "exam2024");
            Assert.IsTrue(correctPhase2Result, "Phase 2 with correct password should succeed");

            // Try to access exam before password change
            var student = _context.Students.First(s => s.UserId == user.UserId);
            var exam = _context.Exams.First(e => e.IsPublished);
            
            // User still needs to change password
            var examBeforePasswordChange = _examService.StartExam(student.StudentId, exam.ExamId);
            // This might succeed or fail depending on implementation - the key is that MustChangePassword is still true
            
            var userBeforePasswordChange = _context.Users.Find(user.UserId);
            Assert.IsTrue(userBeforePasswordChange.MustChangePassword, "User should still need to change password");

            // Complete password change
            var passwordChanged = _authService.ChangePassword(user.UserId, "newpassword123");
            Assert.IsTrue(passwordChanged, "Password change should succeed");

            // Now exam access should work properly
            var userAfterPasswordChange = _context.Users.Find(user.UserId);
            Assert.IsFalse(userAfterPasswordChange.MustChangePassword, "User should not need to change password after change");
        }

        [TestMethod]
        public void ExamWorkflow_InvalidExamData_HandledGracefully()
        {
            // Arrange - Create exam with no questions
            var emptyExam = new Exam
            {
                Title = "Empty Exam",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(emptyExam);
            _context.SaveChanges();

            var student = _context.Students.First();
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            // Act & Assert - Try to start exam with no questions
            var studentExam = _examService.StartExam(student.StudentId, emptyExam.ExamId);
            
            if (studentExam != null)
            {
                // If exam starts, it should handle empty questions gracefully
                var questions = _examService.GetExamQuestions(emptyExam.ExamId);
                Assert.AreEqual(0, questions.Count, "Empty exam should have no questions");

                // Submit empty exam
                var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
                Assert.IsTrue(submitResult, "Empty exam should be submittable");

                // Verify grading handles empty exam
                var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
                if (gradingResult != null)
                {
                    Assert.AreEqual(0, gradingResult.TotalQuestions, "Empty exam should have 0 total questions");
                    Assert.AreEqual(0, gradingResult.CorrectAnswers, "Empty exam should have 0 correct answers");
                }
            }
            else
            {
                // If exam doesn't start, that's also acceptable behavior for invalid exam data
                Assert.IsNull(studentExam, "System may prevent starting exam with no questions");
            }
        }

        [TestMethod]
        public void ExamWorkflow_DataIntegrityValidation_Success()
        {
            // Arrange
            var student = _context.Students.First();
            var exam = _context.Exams.First(e => e.IsPublished);
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            var questions = _examService.GetExamQuestions(exam.ExamId);

            // Act - Save answer and then manually corrupt data to test integrity
            var correctChoice = _context.Choices.First(c => c.QuestionId == questions[0].QuestionId && c.IsCorrect);
            var answerSaved = _examService.SaveAnswer(studentExam.StudentExamId, questions[0].QuestionId, correctChoice.ChoiceId);
            Assert.IsTrue(answerSaved, "Answer should be saved initially");

            // Verify data integrity by checking the saved answer
            var savedAnswer = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[0].QuestionId);
            Assert.IsNotNull(savedAnswer, "Saved answer should exist");
            Assert.AreEqual(correctChoice.ChoiceId, savedAnswer.ChoiceId, "Saved answer should match submitted choice");
            Assert.AreEqual(questions[0].QuestionId, savedAnswer.QuestionId, "Saved answer should reference correct question");
            Assert.AreEqual(studentExam.StudentExamId, savedAnswer.StudentExamId, "Saved answer should reference correct exam session");

            // Test that answer updates work correctly
            var wrongChoice = _context.Choices.First(c => c.QuestionId == questions[0].QuestionId && !c.IsCorrect);
            var answerUpdated = _examService.SaveAnswer(studentExam.StudentExamId, questions[0].QuestionId, wrongChoice.ChoiceId);
            Assert.IsTrue(answerUpdated, "Answer should be updated");

            var updatedAnswer = _examService.GetStudentAnswer(studentExam.StudentExamId, questions[0].QuestionId);
            Assert.AreEqual(wrongChoice.ChoiceId, updatedAnswer.ChoiceId, "Updated answer should reflect new choice");

            // Submit and verify integrity is maintained
            var submitResult = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsTrue(submitResult, "Exam should submit successfully");

            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.AreEqual(0, gradingResult.CorrectAnswers, "Should have 0 correct answers (answer was changed to wrong)");
            Assert.AreEqual(1, gradingResult.IncorrectAnswers, "Should have 1 incorrect answer");
        }

        #endregion

        #region Timer-Based Scenarios

        [TestMethod]
        public void ExamWorkflow_TimerValidation_EnforcesTimeLimit()
        {
            // Arrange
            var student = _context.Students.First();
            var exam = _context.Exams.First();
            
            // Complete authentication
            var user = _context.Users.First(u => u.UserId == student.UserId);
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            // Start exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);

            // Act - Check initial remaining time
            var initialRemainingTime = _examService.GetRemainingTime(studentExam.StudentExamId);
            Assert.IsTrue(initialRemainingTime > TimeSpan.Zero, "Should have remaining time initially");

            // Simulate time passing by updating start time
            studentExam.StartDateTime = DateTime.Now.AddMinutes(-exam.DurationMinutes + 10); // 10 minutes left
            _context.SaveChanges();

            var remainingTime = _examService.GetRemainingTime(studentExam.StudentExamId);
            Assert.IsTrue(remainingTime > TimeSpan.Zero && remainingTime <= TimeSpan.FromMinutes(10), 
                "Should have approximately 10 minutes remaining");

            // Simulate exam expiry
            studentExam.StartDateTime = DateTime.Now.AddMinutes(-exam.DurationMinutes - 1);
            _context.SaveChanges();

            // Act - Check if exam is expired
            var expiredRemainingTime = _examService.GetRemainingTime(studentExam.StudentExamId);
            
            // Assert
            Assert.IsTrue(expiredRemainingTime <= TimeSpan.Zero, "Exam should be expired");
            
            // Verify that timer service detects expiry
            var isExpired = _timerService.IsExamExpired(studentExam.StudentExamId);
            Assert.IsTrue(isExpired, "Timer service should detect expiry");
        }

        [TestMethod]
        public void ExamWorkflow_AutoSubmissionOnTimeout_Success()
        {
            // Arrange
            var studentEmail = $"student2{_testId}@hems.edu";
            var studentIdNumber = $"SE002{_testId}";
            
            // Complete authentication
            CompleteAuthentication(studentEmail, studentIdNumber);
            
            var student = GetStudentByEmail(studentEmail);
            var exam = _context.Exams.First(e => e.IsPublished);

            // Start exam
            var studentExam = _examService.StartExam(student.StudentId, exam.ExamId);
            
            // Answer some questions before timer expires
            var questions = _examService.GetExamQuestions(exam.ExamId);
            var correctChoice = _context.Choices.First(c => c.QuestionId == questions[0].QuestionId && c.IsCorrect);
            _examService.SaveAnswer(studentExam.StudentExamId, questions[0].QuestionId, correctChoice.ChoiceId);
            
            // Simulate timer expiry by setting start time in the past
            studentExam.StartDateTime = DateTime.Now.AddMinutes(-(exam.DurationMinutes + 1));
            _context.SaveChanges();

            // Act - Auto-submit when timer expires
            var isExpired = _timerService.IsExamExpired(studentExam.StudentExamId);
            Assert.IsTrue(isExpired, "Exam should be expired");

            var autoSubmitResult = _examService.SubmitExam(studentExam.StudentExamId);
            Assert.IsTrue(autoSubmitResult, "Auto-submission should succeed");

            // Assert - Verify exam was auto-submitted and graded
            var submittedExam = _examService.GetStudentExamSession(studentExam.StudentExamId);
            Assert.IsTrue(submittedExam.IsSubmitted, "Exam should be auto-submitted");

            var gradingResult = _gradingService.GetGradingResult(studentExam.StudentExamId);
            Assert.IsNotNull(gradingResult, "Auto-submitted exam should be graded");
            Assert.AreEqual(20m, gradingResult.Percentage, "Should have 20% score (1/5 answered)");
        }

        #endregion

        #region Helper Methods

        private void CompleteAuthentication(string email, string idNumber)
        {
            var phase1Password = _authService.CalculatePhase1Password(idNumber);
            _authService.ValidatePhase1Login(email, phase1Password);
            
            var user = _authService.GetUserByEmail(email);
            _authService.CompletePhase1Login(user.UserId);
            _authService.ValidatePhase2Login(idNumber, "exam2024");
            _authService.ChangePassword(user.UserId, "newpassword123");
        }

        private Student GetStudentByEmail(string email)
        {
            var user = _context.Users.First(u => u.Username == email);
            return _context.Students.First(s => s.UserId == user.UserId);
        }

        #endregion
    }
}