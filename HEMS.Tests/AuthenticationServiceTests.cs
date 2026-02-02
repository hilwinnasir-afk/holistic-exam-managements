using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Services;
using HEMS.Models;
using System.Data.Entity;
using Moq;
using BCrypt.Net;

namespace HEMS.Tests
{
    [TestClass]
    public class AuthenticationServiceTests
    {
        private AuthenticationService _authService;
        private Mock<HEMSContext> _mockContext;
        private Mock<DbSet<User>> _mockUserSet;
        private Mock<DbSet<Student>> _mockStudentSet;
        private Mock<DbSet<ExamSession>> _mockExamSessionSet;
        private Mock<DbSet<LoginSession>> _mockLoginSessionSet;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _mockUserSet = new Mock<DbSet<User>>();
            _mockStudentSet = new Mock<DbSet<Student>>();
            _mockExamSessionSet = new Mock<DbSet<ExamSession>>();
            _mockLoginSessionSet = new Mock<DbSet<LoginSession>>();
            
            _mockContext.Setup(c => c.Users).Returns(_mockUserSet.Object);
            _mockContext.Setup(c => c.Students).Returns(_mockStudentSet.Object);
            _mockContext.Setup(c => c.ExamSessions).Returns(_mockExamSessionSet.Object);
            _mockContext.Setup(c => c.LoginSessions).Returns(_mockLoginSessionSet.Object);
            
            _authService = new AuthenticationService(_mockContext.Object);
        }

        [TestMethod]
        public void CalculatePhase1Password_ValidIdNumber_ReturnsCorrectPassword()
        {
            // Arrange
            string idNumber = "SE123";
            
            // Act
            string result = _authService.CalculatePhase1Password(idNumber);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.StartsWith(idNumber));
            Assert.AreEqual(idNumber.Length + 2, result.Length); // ID + 2 digits
            
            // Verify it contains the Ethiopian year calculation
            var currentYear = DateTime.Now.Year;
            var ethiopianYear = currentYear - 7;
            var expectedSuffix = (ethiopianYear % 100).ToString("D2");
            Assert.IsTrue(result.EndsWith(expectedSuffix));
        }

        [TestMethod]
        public void CalculatePhase1Password_EmptyIdNumber_ReturnsEmptyString()
        {
            // Arrange
            string idNumber = "";
            
            // Act
            string result = _authService.CalculatePhase1Password(idNumber);
            
            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void CalculatePhase1Password_NullIdNumber_ReturnsEmptyString()
        {
            // Arrange
            string idNumber = null;
            
            // Act
            string result = _authService.CalculatePhase1Password(idNumber);
            
            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void CalculatePhase1Password_WhitespaceIdNumber_ReturnsEmptyString()
        {
            // Arrange
            string idNumber = "   ";
            
            // Act
            string result = _authService.CalculatePhase1Password(idNumber);
            
            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_ValidUniversityEmail_ReturnsTrue()
        {
            // Arrange
            string email = "student@hems.edu";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_InvalidEmail_ReturnsFalse()
        {
            // Arrange
            string email = "student@gmail.com";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_EmptyEmail_ReturnsFalse()
        {
            // Arrange
            string email = "";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_NullEmail_ReturnsFalse()
        {
            // Arrange
            string email = null;
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_EthiopianEducationalDomain_ReturnsTrue()
        {
            // Arrange
            string email = "student@edu.et";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_AddisAbabaUniversity_ReturnsTrue()
        {
            // Arrange
            string email = "student@aau.edu.et";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_InvalidEmailFormat_ReturnsFalse()
        {
            // Arrange
            string email = "invalid-email-format";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsUniversityEmailValid_MissingAtSymbol_ReturnsFalse()
        {
            // Arrange
            string email = "studenthems.edu";
            
            // Act
            bool result = _authService.IsUniversityEmailValid(email);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _authService?.Dispose();
        }

        [TestMethod]
        public void ValidatePhase1Login_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var email = "student@hems.edu";
            var idNumber = "SE123";
            var expectedPassword = _authService.CalculatePhase1Password(idNumber);
            
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber, 
                UniversityEmail = email 
            };
            
            var user = new User 
            { 
                UserId = 1, 
                Username = email, 
                LoginPhaseCompleted = false,
                Student = student  // Set up the navigation property
            };

            var users = new List<User> { user }.AsQueryable();
            var students = new List<Student> { student }.AsQueryable();

            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase1Login(email, expectedPassword);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_EmptyEmail_ReturnsFalse()
        {
            // Arrange
            var email = "";
            var password = "SE12317";
            
            // Act
            bool result = _authService.ValidatePhase1Login(email, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_EmptyPassword_ReturnsFalse()
        {
            // Arrange
            var email = "student@hems.edu";
            var password = "";
            
            // Act
            bool result = _authService.ValidatePhase1Login(email, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_InvalidEmailFormat_ReturnsFalse()
        {
            // Arrange
            var email = "invalid-email";
            var password = "SE12317";
            
            // Act
            bool result = _authService.ValidatePhase1Login(email, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_NonUniversityDomain_ReturnsFalse()
        {
            // Arrange
            var email = "student@gmail.com";
            var password = "SE12317";
            
            // Act
            bool result = _authService.ValidatePhase1Login(email, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var email = "nonexistent@hems.edu";
            var password = "SE12317";
            
            var users = new List<User>().AsQueryable();
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase1Login(email, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_Phase1AlreadyCompleted_ReturnsFalse()
        {
            // Arrange
            var email = "student@hems.edu";
            var password = "SE12317";
            
            var user = new User 
            { 
                UserId = 1, 
                Username = email, 
                LoginPhaseCompleted = true // Already completed
            };

            var users = new List<User> { user }.AsQueryable();
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase1Login(email, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase1Login_WrongPassword_ReturnsFalse()
        {
            // Arrange
            var email = "student@hems.edu";
            var idNumber = "SE123";
            var wrongPassword = "wrongpassword";
            
            var user = new User 
            { 
                UserId = 1, 
                Username = email, 
                LoginPhaseCompleted = false 
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber, 
                UniversityEmail = email 
            };

            var users = new List<User> { user }.AsQueryable();
            var students = new List<Student> { student }.AsQueryable();

            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase1Login(email, wrongPassword);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase2Login_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var idNumber = "SE123";
            var sessionPassword = "exam2024";
            
            var user = new User 
            { 
                UserId = 1, 
                LoginPhaseCompleted = true // Phase 1 completed
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber,
                User = user
            };
            var examSession = new ExamSession
            {
                ExamSessionId = 1,
                ExamId = 1,
                SessionPassword = BCrypt.Net.BCrypt.HashPassword(sessionPassword), // Hash the password
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(2) // Active session
            };

            var students = new List<Student> { student }.AsQueryable();
            var examSessions = new List<ExamSession> { examSession }.AsQueryable();

            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Provider).Returns(examSessions.Provider);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Expression).Returns(examSessions.Expression);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.ElementType).Returns(examSessions.ElementType);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.GetEnumerator()).Returns(examSessions.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase2Login(idNumber, sessionPassword);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidatePhase2Login_EmptyIdNumber_ReturnsFalse()
        {
            // Arrange
            var idNumber = "";
            var password = "exam2024";
            
            // Act
            bool result = _authService.ValidatePhase2Login(idNumber, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase2Login_EmptyPassword_ReturnsFalse()
        {
            // Arrange
            var idNumber = "SE123";
            var password = "";
            
            // Act
            bool result = _authService.ValidatePhase2Login(idNumber, password);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase2Login_Phase1NotCompleted_ReturnsFalse()
        {
            // Arrange
            var idNumber = "SE123";
            var sessionPassword = "exam2024";
            
            var user = new User 
            { 
                UserId = 1, 
                LoginPhaseCompleted = false // Phase 1 not completed
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber,
                User = user
            };

            var students = new List<Student> { student }.AsQueryable();
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase2Login(idNumber, sessionPassword);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase2Login_NoActiveSession_ReturnsFalse()
        {
            // Arrange
            var idNumber = "SE123";
            var sessionPassword = "exam2024";
            
            var user = new User 
            { 
                UserId = 1, 
                LoginPhaseCompleted = true
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber,
                User = user
            };

            var students = new List<Student> { student }.AsQueryable();
            var examSessions = new List<ExamSession>().AsQueryable(); // No active sessions

            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Provider).Returns(examSessions.Provider);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Expression).Returns(examSessions.Expression);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.ElementType).Returns(examSessions.ElementType);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.GetEnumerator()).Returns(examSessions.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase2Login(idNumber, sessionPassword);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidatePhase2Login_ExpiredSession_ReturnsFalse()
        {
            // Arrange
            var idNumber = "SE123";
            var sessionPassword = "exam2024";
            
            var user = new User 
            { 
                UserId = 1, 
                LoginPhaseCompleted = true
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber,
                User = user
            };
            var examSession = new ExamSession
            {
                ExamSessionId = 1,
                ExamId = 1,
                SessionPassword = sessionPassword,
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(-1) // Expired session
            };

            var students = new List<Student> { student }.AsQueryable();
            var examSessions = new List<ExamSession> { examSession }.AsQueryable();

            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(students.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(students.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(students.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(students.GetEnumerator());

            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Provider).Returns(examSessions.Provider);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Expression).Returns(examSessions.Expression);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.ElementType).Returns(examSessions.ElementType);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.GetEnumerator()).Returns(examSessions.GetEnumerator());

            // Act
            bool result = _authService.ValidatePhase2Login(idNumber, sessionPassword);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CompletePhase1Login_ValidUser_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                LoginPhaseCompleted = false 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.CompletePhase1Login(userId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(user.LoginPhaseCompleted);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void CompletePhase1Login_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var userId = 999;
            _mockContext.Setup(c => c.Users.Find(userId)).Returns((User)null);

            // Act
            bool result = _authService.CompletePhase1Login(userId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CompletePhase1Login_AlreadyCompleted_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                LoginPhaseCompleted = true // Already completed
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.CompletePhase1Login(userId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ChangePassword_ValidUser_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            var newPassword = "newpassword123";
            var user = new User 
            { 
                UserId = userId, 
                MustChangePassword = true 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.ChangePassword(userId, newPassword);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(user.MustChangePassword);
            Assert.IsNotNull(user.PasswordHash);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void ChangePassword_EmptyPassword_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var newPassword = "";

            // Act
            bool result = _authService.ChangePassword(userId, newPassword);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ChangePassword_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var userId = 999;
            var newPassword = "newpassword123";
            _mockContext.Setup(c => c.Users.Find(userId)).Returns((User)null);

            // Act
            bool result = _authService.ChangePassword(userId, newPassword);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPhase1Completed_CompletedUser_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                LoginPhaseCompleted = true 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.IsPhase1Completed(userId);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsPhase1Completed_NotCompletedUser_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                LoginPhaseCompleted = false 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.IsPhase1Completed(userId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPhase1Completed_UserNotFound_ReturnsFalse()
        {
            // Arrange
            var userId = 999;
            _mockContext.Setup(c => c.Users.Find(userId)).Returns((User)null);

            // Act
            bool result = _authService.IsPhase1Completed(userId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MustChangePassword_UserMustChange_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                MustChangePassword = true 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.MustChangePassword(userId);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MustChangePassword_UserDoesNotNeedChange_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                MustChangePassword = false 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act
            bool result = _authService.MustChangePassword(userId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void MustChangePassword_UserNotFound_ReturnsTrue()
        {
            // Arrange
            var userId = 999;
            _mockContext.Setup(c => c.Users.Find(userId)).Returns((User)null);

            // Act
            bool result = _authService.MustChangePassword(userId);
            
            // Assert
            Assert.IsTrue(result); // Default to requiring password change on error
        }

        [TestMethod]
        public void CreateLoginSession_ValidPhase1Session_ReturnsSessionToken()
        {
            // Arrange
            var userId = 1;
            var loginPhase = 1;
            var ipAddress = "192.168.1.1";
            var userAgent = "Mozilla/5.0";

            _mockContext.Setup(c => c.LoginSessions.Add(It.IsAny<LoginSession>()));

            // Act
            string result = _authService.CreateLoginSession(userId, loginPhase, ipAddress, userAgent);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(32, result.Length); // GUID without hyphens
            _mockContext.Verify(c => c.LoginSessions.Add(It.IsAny<LoginSession>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void CreateLoginSession_ValidPhase2Session_ReturnsSessionToken()
        {
            // Arrange
            var userId = 1;
            var loginPhase = 2;
            var ipAddress = "192.168.1.1";
            var userAgent = "Mozilla/5.0";
            var examSessionId = 1;

            var user = new User { UserId = userId };
            var existingSessions = new List<LoginSession>().AsQueryable();

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);
            _mockContext.Setup(c => c.LoginSessions.Add(It.IsAny<LoginSession>()));
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(existingSessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(existingSessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(existingSessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(existingSessions.GetEnumerator());

            // Act
            string result = _authService.CreateLoginSession(userId, loginPhase, ipAddress, userAgent, examSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(32, result.Length);
            Assert.AreEqual(examSessionId, user.CurrentExamSessionId);
            Assert.IsNotNull(user.LastPhase2Login);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void ValidateLoginSession_ValidSession_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            var sessionToken = "validtoken123";
            
            var loginSession = new LoginSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                IsActive = true
            };

            var sessions = new List<LoginSession> { loginSession }.AsQueryable();
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            bool result = _authService.ValidateLoginSession(userId, sessionToken);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ValidateLoginSession_InvalidSession_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var sessionToken = "invalidtoken";
            
            var sessions = new List<LoginSession>().AsQueryable(); // No sessions
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            bool result = _authService.ValidateLoginSession(userId, sessionToken);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ValidateLoginSession_EmptyToken_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var sessionToken = "";

            // Act
            bool result = _authService.ValidateLoginSession(userId, sessionToken);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasActivePhase2Session_UserHasActiveSession_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            
            var loginSession = new LoginSession
            {
                UserId = userId,
                LoginPhase = 2,
                IsActive = true
            };

            var sessions = new List<LoginSession> { loginSession }.AsQueryable();
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            bool result = _authService.HasActivePhase2Session(userId);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasActivePhase2Session_UserHasNoActiveSession_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            
            var sessions = new List<LoginSession>().AsQueryable(); // No sessions
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            bool result = _authService.HasActivePhase2Session(userId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CanStartPhase2Session_NoExistingSession_ReturnsTrue()
        {
            // Arrange
            var userId = 1;
            var examSessionId = 1;
            
            var sessions = new List<LoginSession>().AsQueryable(); // No existing sessions
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            bool result = _authService.CanStartPhase2Session(userId, examSessionId);
            
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CanStartPhase2Session_ExistingActiveSession_ReturnsFalse()
        {
            // Arrange
            var userId = 1;
            var examSessionId = 1;
            
            var loginSession = new LoginSession
            {
                UserId = userId,
                LoginPhase = 2,
                ExamSessionId = examSessionId,
                IsActive = true
            };

            var sessions = new List<LoginSession> { loginSession }.AsQueryable();
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            bool result = _authService.CanStartPhase2Session(userId, examSessionId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void InvalidateLoginSession_ValidSession_InvalidatesSession()
        {
            // Arrange
            var userId = 1;
            var sessionToken = "validtoken123";
            
            var loginSession = new LoginSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                IsActive = true
            };

            var sessions = new List<LoginSession> { loginSession }.AsQueryable();
            
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            _authService.InvalidateLoginSession(userId, sessionToken);
            
            // Assert
            Assert.IsFalse(loginSession.IsActive);
            Assert.IsNotNull(loginSession.LogoutTime);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void InvalidateAllUserSessions_ValidUser_InvalidatesAllSessions()
        {
            // Arrange
            var userId = 1;
            var user = new User { UserId = userId, CurrentExamSessionId = 1 };
            
            var session1 = new LoginSession { UserId = userId, IsActive = true };
            var session2 = new LoginSession { UserId = userId, IsActive = true };
            var sessions = new List<LoginSession> { session1, session2 }.AsQueryable();
            
            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Provider).Returns(sessions.Provider);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.Expression).Returns(sessions.Expression);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.ElementType).Returns(sessions.ElementType);
            _mockLoginSessionSet.As<IQueryable<LoginSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

            // Act
            _authService.InvalidateAllUserSessions(userId);
            
            // Assert
            Assert.IsFalse(session1.IsActive);
            Assert.IsFalse(session2.IsActive);
            Assert.IsNotNull(session1.LogoutTime);
            Assert.IsNotNull(session2.LogoutTime);
            Assert.IsNull(user.CurrentExamSessionId);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }
    }
}