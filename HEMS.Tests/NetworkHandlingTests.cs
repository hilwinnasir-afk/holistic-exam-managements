using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Controllers;
using HEMS.Models;
using HEMS.Services;
using System.Data.Entity;

namespace HEMS.Tests
{
    /// <summary>
    /// Tests for network handling functionality during exam taking
    /// </summary>
    [TestClass]
    public class NetworkHandlingTests
    {
        private HEMSContext _context;
        private ExamController _controller;
        private IExamService _examService;
        private ITimerService _timerService;
        private IGradingService _gradingService;
        private IAuditService _auditService;
        private IDataIntegrityService _dataIntegrityService;

        [TestInitialize]
        public void Setup()
        {
            // Use in-memory database for testing
            Database.SetInitializer(new DropCreateDatabaseAlways<HEMSContext>());
            _context = new HEMSContext();
            _context.Database.Initialize(true);

            // Initialize services
            _examService = new ExamService(_context);
            _timerService = new TimerService(_context);
            _gradingService = new GradingService(_context);
            _auditService = new AuditService(_context);
            _dataIntegrityService = new DataIntegrityService(_context);

            // Initialize controller
            _controller = new ExamController(_context, _examService, _timerService, _gradingService, _auditService, _dataIntegrityService);

            // Set up test data
            SetupTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _controller?.Dispose();
            _context?.Dispose();
        }

        private void SetupTestData()
        {
            // Create roles
            var studentRole = new Role { RoleName = "Student" };
            var coordinatorRole = new Role { RoleName = "Coordinator" };
            _context.Roles.Add(studentRole);
            _context.Roles.Add(coordinatorRole);

            // Create test user and student
            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                Role = studentRole,
                LoginPhaseCompleted = true,
                MustChangePassword = false
            };
            _context.Users.Add(user);

            var student = new Student
            {
                User = user,
                IdNumber = "12345",
                UniversityEmail = "test@university.edu",
                BatchYear = 2024
            };
            _context.Students.Add(student);

            // Create test exam
            var exam = new Exam
            {
                Title = "Test Exam",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);

            // Create test questions and choices
            var question1 = new Question
            {
                Exam = exam,
                QuestionText = "What is 2 + 2?",
                QuestionOrder = 1
            };
            _context.Questions.Add(question1);

            var choice1 = new Choice
            {
                Question = question1,
                ChoiceText = "3",
                IsCorrect = false,
                ChoiceOrder = 1
            };
            var choice2 = new Choice
            {
                Question = question1,
                ChoiceText = "4",
                IsCorrect = true,
                ChoiceOrder = 2
            };
            _context.Choices.Add(choice1);
            _context.Choices.Add(choice2);

            // Create student exam session
            var studentExam = new StudentExam
            {
                Student = student,
                Exam = exam,
                StartDateTime = DateTime.Now,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);

            // Create student answer
            var studentAnswer = new StudentAnswer
            {
                StudentExam = studentExam,
                Question = question1,
                IsFlagged = false,
                LastModified = DateTime.Now
            };
            _context.StudentAnswers.Add(studentAnswer);

            _context.SaveChanges();
        }

        [TestMethod]
        public void NetworkHeartbeat_WithValidSession_ReturnsSuccess()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            // Act
            var result = _controller.NetworkHeartbeat() as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);
            Assert.AreEqual("Server connection active", data.message);
            Assert.AreEqual(studentExam.StudentExamId, data.studentExamId);
        }

        [TestMethod]
        public void NetworkHeartbeat_WithNoSession_ReturnsFailure()
        {
            // Arrange
            // No session set

            // Act
            var result = _controller.NetworkHeartbeat() as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.AreEqual("No active exam session", data.message);
            Assert.IsTrue(data.requiresLogin);
        }

        [TestMethod]
        public void NetworkHeartbeat_WithSubmittedExam_ReturnsExamSubmitted()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            studentExam.IsSubmitted = true;
            _context.SaveChanges();

            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            // Act
            var result = _controller.NetworkHeartbeat() as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.AreEqual("Exam already submitted", data.message);
            Assert.IsTrue(data.examSubmitted);
            Assert.IsNotNull(data.redirectUrl);
        }

        [TestMethod]
        public void SyncOfflineData_WithValidAnswerData_SyncsSuccessfully()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            var question = _context.Questions.First();
            var choice = _context.Choices.First(c => c.IsCorrect);

            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = question.QuestionId,
                    ChoiceId = choice.ChoiceId,
                    Timestamp = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);
            Assert.AreEqual(1, data.syncedCount);
            Assert.AreEqual(0, data.failedCount);

            // Verify answer was synced
            var studentAnswer = _context.StudentAnswers
                .First(sa => sa.StudentExamId == studentExam.StudentExamId && sa.QuestionId == question.QuestionId);
            Assert.AreEqual(choice.ChoiceId, studentAnswer.ChoiceId);
        }

        [TestMethod]
        public void SyncOfflineData_WithValidFlagData_SyncsSuccessfully()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            var question = _context.Questions.First();

            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "flag",
                    QuestionId = question.QuestionId,
                    IsFlagged = true,
                    Timestamp = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);
            Assert.AreEqual(1, data.syncedCount);
            Assert.AreEqual(0, data.failedCount);

            // Verify flag was synced
            var studentAnswer = _context.StudentAnswers
                .First(sa => sa.StudentExamId == studentExam.StudentExamId && sa.QuestionId == question.QuestionId);
            Assert.IsTrue(studentAnswer.IsFlagged);
        }

        [TestMethod]
        public void SyncOfflineData_WithOlderTimestamp_SkipsSync()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            var question = _context.Questions.First();
            var choice = _context.Choices.First(c => c.IsCorrect);

            // Set current answer with recent timestamp
            var studentAnswer = _context.StudentAnswers
                .First(sa => sa.StudentExamId == studentExam.StudentExamId && sa.QuestionId == question.QuestionId);
            studentAnswer.ChoiceId = choice.ChoiceId;
            studentAnswer.LastModified = DateTime.Now;
            _context.SaveChanges();

            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = question.QuestionId,
                    ChoiceId = _context.Choices.First(c => !c.IsCorrect).ChoiceId, // Different choice
                    Timestamp = DateTime.Now.AddMinutes(-10).ToString("yyyy-MM-dd HH:mm:ss"), // Older timestamp
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);
            Assert.AreEqual(0, data.syncedCount);
            Assert.AreEqual(1, data.failedCount);

            // Verify original answer was not changed
            _context.Entry(studentAnswer).Reload();
            Assert.AreEqual(choice.ChoiceId, studentAnswer.ChoiceId);
        }

        [TestMethod]
        public void SyncOfflineData_WithNoSession_ReturnsFailure()
        {
            // Arrange
            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = 1,
                    ChoiceId = 1,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.AreEqual("No active exam session", data.message);
            Assert.IsTrue(data.requiresLogin);
        }

        [TestMethod]
        public void SyncOfflineData_WithEmptyItems_ReturnsSuccess()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var offlineItems = new List<OfflineDataItem>();

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);
            Assert.AreEqual("No items to sync", data.message);
            Assert.AreEqual(0, data.syncedCount);
        }

        [TestMethod]
        public void SyncOfflineData_WithInvalidQuestionId_ReturnsFailure()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = 99999, // Invalid question ID
                    ChoiceId = 1,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success); // Overall success but individual item failed
            Assert.AreEqual(0, data.syncedCount);
            Assert.AreEqual(1, data.failedCount);
        }

        [TestMethod]
        public void SyncOfflineData_WithMixedValidAndInvalidItems_ProcessesBoth()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            var question = _context.Questions.First();
            var choice = _context.Choices.First(c => c.IsCorrect);

            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = question.QuestionId,
                    ChoiceId = choice.ChoiceId,
                    Timestamp = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                },
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = 99999, // Invalid question ID
                    ChoiceId = 1,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);
            Assert.AreEqual(1, data.syncedCount);
            Assert.AreEqual(1, data.failedCount);

            // Verify valid answer was synced
            var studentAnswer = _context.StudentAnswers
                .First(sa => sa.StudentExamId == studentExam.StudentExamId && sa.QuestionId == question.QuestionId);
            Assert.AreEqual(choice.ChoiceId, studentAnswer.ChoiceId);
        }

        [TestMethod]
        public void SyncOfflineData_LogsAuditEvents()
        {
            // Arrange
            var studentExam = _context.StudentExams.First();
            var question = _context.Questions.First();
            var choice = _context.Choices.First(c => c.IsCorrect);

            _controller.Session["CurrentStudentExamId"] = studentExam.StudentExamId;
            _controller.Session["CurrentStudentId"] = studentExam.StudentId;

            var initialAuditCount = _context.AuditLogs.Count();

            var offlineItems = new List<OfflineDataItem>
            {
                new OfflineDataItem
                {
                    Type = "answer",
                    QuestionId = question.QuestionId,
                    ChoiceId = choice.ChoiceId,
                    Timestamp = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss"),
                    Attempts = 1,
                    Synced = false
                }
            };

            // Act
            var result = _controller.SyncOfflineData(offlineItems) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsTrue(data.success);

            // Verify audit logs were created
            var finalAuditCount = _context.AuditLogs.Count();
            Assert.IsTrue(finalAuditCount > initialAuditCount, "Audit logs should be created for sync operations");

            // Check for specific audit events
            var syncAuditLog = _context.AuditLogs
                .FirstOrDefault(al => al.EventType == AuditEventTypes.DataSynced);
            Assert.IsNotNull(syncAuditLog, "DataSynced audit event should be logged");

            var answerSyncAuditLog = _context.AuditLogs
                .FirstOrDefault(al => al.EventType == AuditEventTypes.AnswerSynced);
            Assert.IsNotNull(answerSyncAuditLog, "AnswerSynced audit event should be logged");
        }
    }
}