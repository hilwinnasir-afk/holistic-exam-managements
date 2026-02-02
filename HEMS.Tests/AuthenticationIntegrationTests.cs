using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Services;
using HEMS.Models;
using BCrypt.Net;

namespace HEMS.Tests
{
    /// <summary>
    /// Integration tests for the complete authentication workflow
    /// Tests the full Phase 1 and Phase 2 authentication process
    /// </summary>
    [TestClass]
    public class AuthenticationIntegrationTests
    {
        private HEMSContext _context;
        private AuthenticationService _authService;

        [TestInitialize]
        public void Setup()
        {
            // Use in-memory database for testing
            Database.SetInitializer(new DropCreateDatabaseAlways<HEMSContext>());
            _context = new HEMSContext();
            _context.Database.Initialize(true);
            _authService = new AuthenticationService(_context);
            
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
            // Clean up existing data to avoid duplicate key errors
            // Order matters due to foreign key constraints
            var examSessions = _context.ExamSessions.ToList();
            _context.ExamSessions.RemoveRange(examSessions);
            
            var existingStudents = _context.Students.ToList();
            _context.Students.RemoveRange(existingStudents);
            
            var existingUsers = _context.Users.ToList();
            _context.Users.RemoveRange(existingUsers);
            
            var existingExams = _context.Exams.ToList();
            _context.Exams.RemoveRange(existingExams);
            
            var existingRoles = _context.Roles.ToList();
            _context.Roles.RemoveRange(existingRoles);
            
            _context.SaveChanges();
        }

        private void SeedTestData()
        {
            // Use unique identifiers to avoid conflicts
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            // Create test roles
            var studentRole = new Role { RoleName = $"Student{testId}" };
            var coordinatorRole = new Role { RoleName = $"Coordinator{testId}" };
            
            _context.Roles.Add(studentRole);
            _context.Roles.Add(coordinatorRole);
            _context.SaveChanges();

            // Create test user
            var user = new User
            {
                Username = $"student{testId}@hems.edu",
                PasswordHash = "hashedpassword", // Use a simple hash for testing
                RoleId = studentRole.RoleId,
                LoginPhaseCompleted = false,
                MustChangePassword = false
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Create test student
            var student = new Student
            {
                IdNumber = $"SE{testId}",
                UniversityEmail = $"student{testId}@hems.edu",
                BatchYear = 2017,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();
        }

        [TestMethod]
        public void CompleteAuthenticationWorkflow_ValidCredentials_Success()
        {
            // Calculate the correct Phase 1 password for current year
            var expectedPassword = _authService.CalculatePhase1Password("SE123");
            
            // Phase 1 Login
            var phase1Result = _authService.ValidatePhase1Login("student@hems.edu", expectedPassword);
            Assert.IsTrue(phase1Result, "Phase 1 login should succeed with valid credentials");

            var user = _authService.GetUserByEmail("student@hems.edu");
            Assert.IsNotNull(user, "User should be found by email");

            var phase1Complete = _authService.CompletePhase1Login(user.UserId);
            Assert.IsTrue(phase1Complete, "Phase 1 completion should succeed");

            // Verify Phase 1 completion
            var updatedUser = _context.Users.Find(user.UserId);
            Assert.AreEqual(true, updatedUser.LoginPhaseCompleted, "LoginPhaseCompleted should be true after Phase 1");

            // Phase 2 Login - need to create an active exam session first
            var exam = new Exam
            {
                Title = "Test Exam",
                AcademicYear = 2024,
                DurationMinutes = 120,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var examSession = new ExamSession
            {
                ExamId = exam.ExamId,
                SessionPassword = BCrypt.Net.BCrypt.HashPassword("exam2024"),
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(2)
            };
            _context.ExamSessions.Add(examSession);
            _context.SaveChanges();

            var phase2Result = _authService.ValidatePhase2Login("SE123", "exam2024");
            Assert.IsTrue(phase2Result, "Phase 2 login should succeed with valid credentials");

            var userByIdNumber = _authService.GetUserByIdNumber("SE123");
            Assert.IsNotNull(userByIdNumber, "User should be found by ID number");
            Assert.AreEqual(user.UserId, userByIdNumber.UserId, "Should be the same user");
        }

        [TestMethod]
        public void Phase1Login_InvalidEmail_Fails()
        {
            var expectedPassword = _authService.CalculatePhase1Password("SE123");
            var result = _authService.ValidatePhase1Login("invalid@hems.edu", expectedPassword);
            Assert.IsFalse(result, "Phase 1 login should fail with invalid email");
        }

        [TestMethod]
        public void Phase1Login_InvalidPassword_Fails()
        {
            var result = _authService.ValidatePhase1Login("student@hems.edu", "wrongpassword");
            Assert.IsFalse(result, "Phase 1 login should fail with invalid password");
        }

        [TestMethod]
        public void Phase2Login_WithoutPhase1_Fails()
        {
            // Try Phase 2 without completing Phase 1
            var result = _authService.ValidatePhase2Login("SE123", "exam2024");
            Assert.IsFalse(result, "Phase 2 login should fail if Phase 1 not completed");
        }

        [TestMethod]
        public void Phase2Login_InvalidIdNumber_Fails()
        {
            // Complete Phase 1 first
            var expectedPassword = _authService.CalculatePhase1Password("SE123");
            _authService.ValidatePhase1Login("student@hems.edu", expectedPassword);
            var user = _authService.GetUserByEmail("student@hems.edu");
            _authService.CompletePhase1Login(user.UserId);

            // Create an active exam session
            var exam = new Exam
            {
                Title = "Test Exam",
                AcademicYear = 2024,
                DurationMinutes = 120,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var examSession = new ExamSession
            {
                ExamId = exam.ExamId,
                SessionPassword = BCrypt.Net.BCrypt.HashPassword("exam2024"),
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(2)
            };
            _context.ExamSessions.Add(examSession);
            _context.SaveChanges();

            // Try Phase 2 with invalid ID number
            var result = _authService.ValidatePhase2Login("INVALID", "exam2024");
            Assert.IsFalse(result, "Phase 2 login should fail with invalid ID number");
        }

        [TestMethod]
        public void Phase2Login_InvalidPassword_Fails()
        {
            // Complete Phase 1 first
            var expectedPassword = _authService.CalculatePhase1Password("SE123");
            _authService.ValidatePhase1Login("student@hems.edu", expectedPassword);
            var user = _authService.GetUserByEmail("student@hems.edu");
            _authService.CompletePhase1Login(user.UserId);

            // Create an active exam session with correct password
            var exam = new Exam
            {
                Title = "Test Exam",
                AcademicYear = 2024,
                DurationMinutes = 120,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var examSession = new ExamSession
            {
                ExamId = exam.ExamId,
                SessionPassword = BCrypt.Net.BCrypt.HashPassword("exam2024"),
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(2)
            };
            _context.ExamSessions.Add(examSession);
            _context.SaveChanges();

            // Try Phase 2 with invalid password
            var result = _authService.ValidatePhase2Login("SE123", "wrongpassword");
            Assert.IsFalse(result, "Phase 2 login should fail with invalid password");
        }
    }
}