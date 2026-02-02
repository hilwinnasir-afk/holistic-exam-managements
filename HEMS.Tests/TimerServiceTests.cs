using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Models;
using HEMS.Services;

namespace HEMS.Tests
{
    [TestClass]
    public class TimerServiceTests
    {
        private HEMSContext _context;
        private TimerService _timerService;

        [TestInitialize]
        public void Setup()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<HEMSContext>());
            _context = new HEMSContext();
            _context.Database.Initialize(true);
            _timerService = new TimerService(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test data to prevent duplicate key errors
            var studentExams = _context.StudentExams.ToList();
            _context.StudentExams.RemoveRange(studentExams);
            
            var students = _context.Students.ToList();
            _context.Students.RemoveRange(students);
            
            var users = _context.Users.ToList();
            _context.Users.RemoveRange(users);
            
            var exams = _context.Exams.ToList();
            _context.Exams.RemoveRange(exams);
            
            var roles = _context.Roles.ToList();
            _context.Roles.RemoveRange(roles);
            
            _context.SaveChanges();
            
            _timerService?.Dispose();
            _context?.Dispose();
        }

        [TestMethod]
        public void GetRemainingTime_ValidExam_ReturnsCorrectTime()
        {
            // Arrange - Use unique identifiers to avoid conflicts
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 120,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            // Create role first
            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            // Create user first
            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId,
                LoginPhaseCompleted = false,
                MustChangePassword = false
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = DateTime.Now.AddMinutes(-30), // Started 30 minutes ago
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var remainingTime = _timerService.GetRemainingTime(studentExam.StudentExamId);

            // Assert
            Assert.IsNotNull(remainingTime);
            Assert.IsTrue(remainingTime.Value.TotalMinutes > 85 && remainingTime.Value.TotalMinutes < 95);
        }

        [TestMethod]
        public void IsExamExpired_ExpiredExam_ReturnsTrue()
        {
            // Arrange - Use unique identifiers to avoid conflicts
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            // Create role first
            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            // Create user first
            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId,
                LoginPhaseCompleted = false,
                MustChangePassword = false
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = DateTime.Now.AddMinutes(-90), // Started 90 minutes ago (expired)
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var isExpired = _timerService.IsExamExpired(studentExam.StudentExamId);

            // Assert
            Assert.IsTrue(isExpired);
        }

        [TestMethod]
        public void FormatTime_ValidTimeSpan_ReturnsFormattedString()
        {
            // Arrange
            var timeSpan = new TimeSpan(1, 23, 45); // 1 hour, 23 minutes, 45 seconds

            // Act
            var formatted = _timerService.FormatTime(timeSpan);

            // Assert
            Assert.AreEqual("01:23:45", formatted);
        }

        [TestMethod]
        public void FormatTime_ZeroTime_ReturnsZeroString()
        {
            // Arrange
            var timeSpan = TimeSpan.Zero;

            // Act
            var formatted = _timerService.FormatTime(timeSpan);

            // Assert
            Assert.AreEqual("00:00:00", formatted);
        }

        [TestMethod]
        public void GetExamDurationMinutes_ValidExam_ReturnsCorrectDuration()
        {
            // Arrange
            var exam = new Exam
            {
                Title = "Test Exam",
                AcademicYear = 2024,
                DurationMinutes = 150,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            // Act
            var duration = _timerService.GetExamDurationMinutes(exam.ExamId);

            // Assert
            Assert.AreEqual(150, duration);
        }

        [TestMethod]
        public void GetExamDurationMinutes_ExamNotFound_ReturnsZero()
        {
            // Arrange
            var nonExistentExamId = 999;

            // Act
            var duration = _timerService.GetExamDurationMinutes(nonExistentExamId);

            // Assert
            Assert.AreEqual(0, duration);
        }

        [TestMethod]
        public void GetExamStartTime_ValidStudentExam_ReturnsStartTime()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-15);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var result = _timerService.GetExamStartTime(studentExam.StudentExamId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(startTime.ToString("yyyy-MM-dd HH:mm:ss"), result.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [TestMethod]
        public void GetExamStartTime_StudentExamNotFound_ReturnsNull()
        {
            // Arrange
            var nonExistentStudentExamId = 999;

            // Act
            var result = _timerService.GetExamStartTime(nonExistentStudentExamId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetElapsedTime_ValidStudentExam_ReturnsElapsedTime()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-25);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var elapsedTime = _timerService.GetElapsedTime(studentExam.StudentExamId);

            // Assert
            Assert.IsNotNull(elapsedTime);
            Assert.IsTrue(elapsedTime.Value.TotalMinutes >= 24 && elapsedTime.Value.TotalMinutes <= 26);
        }

        [TestMethod]
        public void GetElapsedTime_StudentExamNotFound_ReturnsNull()
        {
            // Arrange
            var nonExistentStudentExamId = 999;

            // Act
            var result = _timerService.GetElapsedTime(nonExistentStudentExamId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ValidateExamTimeIntegrity_ValidTime_ReturnsTrue()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-10);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            var clientReportedTime = TimeSpan.FromMinutes(10); // Client reports 10 minutes elapsed

            // Act
            var isValid = _timerService.ValidateExamTimeIntegrity(studentExam.StudentExamId, clientReportedTime);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void ValidateExamTimeIntegrity_InvalidTime_ReturnsFalse()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-10);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            var clientReportedTime = TimeSpan.FromMinutes(5); // Client reports only 5 minutes elapsed (suspicious)

            // Act
            var isValid = _timerService.ValidateExamTimeIntegrity(studentExam.StudentExamId, clientReportedTime);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void GetSecureTimestamp_ValidStudentExam_ReturnsSecureTimestamp()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-20);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 90,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var secureTimestamp = _timerService.GetSecureTimestamp(studentExam.StudentExamId);

            // Assert
            Assert.IsNotNull(secureTimestamp);
            Assert.IsNotNull(secureTimestamp.Hash);
            Assert.IsFalse(secureTimestamp.IsExpired);
            Assert.IsTrue(secureTimestamp.TotalSecondsRemaining > 0);
            Assert.IsNotNull(secureTimestamp.FormattedRemainingTime);
        }

        [TestMethod]
        public void GetSecureTimestamp_ExpiredExam_ReturnsExpiredTimestamp()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-120); // Started 2 hours ago
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60, // 1 hour duration (expired)
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var secureTimestamp = _timerService.GetSecureTimestamp(studentExam.StudentExamId);

            // Assert
            Assert.IsNotNull(secureTimestamp);
            Assert.IsTrue(secureTimestamp.IsExpired);
            Assert.AreEqual(0, secureTimestamp.TotalSecondsRemaining);
        }

        [TestMethod]
        public void ValidateTimestampHash_ValidHash_ReturnsTrue()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-15);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Get a valid timestamp and hash
            var secureTimestamp = _timerService.GetSecureTimestamp(studentExam.StudentExamId);
            
            // Act
            var isValid = _timerService.ValidateTimestampHash(studentExam.StudentExamId, secureTimestamp.ServerTime, secureTimestamp.Hash);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void ValidateTimestampHash_InvalidHash_ReturnsFalse()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-15);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            var timestamp = DateTime.Now;
            var invalidHash = "invalid_hash_value";
            
            // Act
            var isValid = _timerService.ValidateTimestampHash(studentExam.StudentExamId, timestamp, invalidHash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void DetectSuspiciousTimingActivity_NormalActivity_ReturnsFalse()
        {
            // Arrange
            var testId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var startTime = DateTime.Now.AddMinutes(-10);
            
            var exam = new Exam
            {
                Title = $"Test Exam {testId}",
                AcademicYear = 2024,
                DurationMinutes = 60,
                IsPublished = true
            };
            _context.Exams.Add(exam);
            _context.SaveChanges();

            var role = new Role { RoleName = $"Student{testId}" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = $"test{testId}@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                IdNumber = $"TEST{testId}",
                UniversityEmail = $"test{testId}@university.edu",
                BatchYear = 2024,
                UserId = user.UserId
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            var studentExam = new StudentExam
            {
                StudentId = student.StudentId,
                ExamId = exam.ExamId,
                StartDateTime = startTime,
                IsSubmitted = false
            };
            _context.StudentExams.Add(studentExam);
            _context.SaveChanges();

            // Act
            var isSuspicious = _timerService.DetectSuspiciousTimingActivity(studentExam.StudentExamId);

            // Assert
            Assert.IsFalse(isSuspicious); // Normal activity should not be suspicious
        }
    }
}