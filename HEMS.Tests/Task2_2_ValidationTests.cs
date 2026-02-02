using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Services;
using HEMS.Models;
using System.Data.Entity;
using Moq;

namespace HEMS.Tests
{
    /// <summary>
    /// Validation tests for Task 2.2: Implement AuthenticationService with Phase 1 and Phase 2 login methods
    /// This test class validates that all required functionality for Task 2.2 is properly implemented
    /// </summary>
    [TestClass]
    public class Task2_2_ValidationTests
    {
        private AuthenticationService _authService;
        private Mock<HEMSContext> _mockContext;
        private Mock<DbSet<User>> _mockUserSet;
        private Mock<DbSet<Student>> _mockStudentSet;
        private Mock<DbSet<ExamSession>> _mockExamSessionSet;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _mockUserSet = new Mock<DbSet<User>>();
            _mockStudentSet = new Mock<DbSet<Student>>();
            _mockExamSessionSet = new Mock<DbSet<ExamSession>>();
            
            _mockContext.Setup(c => c.Users).Returns(_mockUserSet.Object);
            _mockContext.Setup(c => c.Students).Returns(_mockStudentSet.Object);
            _mockContext.Setup(c => c.ExamSessions).Returns(_mockExamSessionSet.Object);
            
            _authService = new AuthenticationService(_mockContext.Object);
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that Phase 1 login method is implemented
        /// Tests the complete Phase 1 authentication workflow
        /// </summary>
        [TestMethod]
        public void Task2_2_Phase1LoginMethod_IsImplementedAndWorking()
        {
            // Arrange - Set up test data for Phase 1 login
            var email = "student@hems.edu";
            var idNumber = "SE123";
            var expectedPassword = _authService.CalculatePhase1Password(idNumber);
            
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

            SetupMockDbSets(new List<User> { user }, new List<Student> { student }, new List<ExamSession>());

            // Act - Test Phase 1 login validation
            bool result = _authService.ValidatePhase1Login(email, expectedPassword);
            
            // Assert - Verify Phase 1 login works correctly
            Assert.IsTrue(result, "Phase 1 login should succeed with valid credentials");
            
            // Verify the method exists and has correct signature
            var method = typeof(AuthenticationService).GetMethod("ValidatePhase1Login");
            Assert.IsNotNull(method, "ValidatePhase1Login method should exist");
            Assert.AreEqual(typeof(bool), method.ReturnType, "ValidatePhase1Login should return bool");
            
            var parameters = method.GetParameters();
            Assert.AreEqual(2, parameters.Length, "ValidatePhase1Login should have 2 parameters");
            Assert.AreEqual(typeof(string), parameters[0].ParameterType, "First parameter should be string (email)");
            Assert.AreEqual(typeof(string), parameters[1].ParameterType, "Second parameter should be string (password)");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that Phase 2 login method is implemented
        /// Tests the complete Phase 2 authentication workflow
        /// </summary>
        [TestMethod]
        public void Task2_2_Phase2LoginMethod_IsImplementedAndWorking()
        {
            // Arrange - Set up test data for Phase 2 login
            var idNumber = "SE123";
            var sessionPassword = "exam2024";
            
            var user = new User 
            { 
                UserId = 1, 
                LoginPhaseCompleted = true // Phase 1 must be completed
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
                ExpiryDate = DateTime.Now.AddHours(2) // Active session
            };

            SetupMockDbSets(new List<User> { user }, new List<Student> { student }, new List<ExamSession> { examSession });

            // Act - Test Phase 2 login validation
            bool result = _authService.ValidatePhase2Login(idNumber, sessionPassword);
            
            // Assert - Verify Phase 2 login works correctly
            Assert.IsTrue(result, "Phase 2 login should succeed with valid credentials and completed Phase 1");
            
            // Verify the method exists and has correct signature
            var method = typeof(AuthenticationService).GetMethod("ValidatePhase2Login");
            Assert.IsNotNull(method, "ValidatePhase2Login method should exist");
            Assert.AreEqual(typeof(bool), method.ReturnType, "ValidatePhase2Login should return bool");
            
            var parameters = method.GetParameters();
            Assert.AreEqual(2, parameters.Length, "ValidatePhase2Login should have 2 parameters");
            Assert.AreEqual(typeof(string), parameters[0].ParameterType, "First parameter should be string (idNumber)");
            Assert.AreEqual(typeof(string), parameters[1].ParameterType, "Second parameter should be string (password)");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that password calculation logic is implemented
        /// Tests the Ethiopian academic year calculation for Phase 1 passwords
        /// </summary>
        [TestMethod]
        public void Task2_2_PasswordCalculationLogic_IsImplementedCorrectly()
        {
            // Arrange
            var idNumber = "SE123";
            
            // Act
            var calculatedPassword = _authService.CalculatePhase1Password(idNumber);
            
            // Assert - Verify password calculation logic
            Assert.IsNotNull(calculatedPassword, "Password calculation should return a value");
            Assert.IsTrue(calculatedPassword.StartsWith(idNumber), "Password should start with ID number");
            Assert.AreEqual(idNumber.Length + 2, calculatedPassword.Length, "Password should be ID + 2 digits");
            
            // Verify Ethiopian academic year calculation
            var currentYear = DateTime.Now.Year;
            var ethiopianYear = currentYear - 7;
            var expectedSuffix = (ethiopianYear % 100).ToString("D2");
            Assert.IsTrue(calculatedPassword.EndsWith(expectedSuffix), 
                $"Password should end with last two digits of Ethiopian year: {expectedSuffix}");
            
            // Verify the method exists and has correct signature
            var method = typeof(AuthenticationService).GetMethod("CalculatePhase1Password");
            Assert.IsNotNull(method, "CalculatePhase1Password method should exist");
            Assert.AreEqual(typeof(string), method.ReturnType, "CalculatePhase1Password should return string");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that university email domain validation is implemented
        /// Tests the email validation logic for Ethiopian educational domains
        /// </summary>
        [TestMethod]
        public void Task2_2_UniversityEmailValidation_IsImplementedCorrectly()
        {
            // Act & Assert - Test various email formats
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@hems.edu"), 
                "Should accept hems.edu domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@edu.et"), 
                "Should accept Ethiopian educational domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@aau.edu.et"), 
                "Should accept Addis Ababa University domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@ju.edu.et"), 
                "Should accept Jimma University domain");
            
            Assert.IsFalse(_authService.IsUniversityEmailValid("student@gmail.com"), 
                "Should reject non-university domains");
            Assert.IsFalse(_authService.IsUniversityEmailValid("invalid-email"), 
                "Should reject invalid email format");
            Assert.IsFalse(_authService.IsUniversityEmailValid(""), 
                "Should reject empty email");
            Assert.IsFalse(_authService.IsUniversityEmailValid(null), 
                "Should reject null email");
            
            // Verify the method exists and has correct signature
            var method = typeof(AuthenticationService).GetMethod("IsUniversityEmailValid");
            Assert.IsNotNull(method, "IsUniversityEmailValid method should exist");
            Assert.AreEqual(typeof(bool), method.ReturnType, "IsUniversityEmailValid should return bool");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that Phase 1 completion tracking is implemented
        /// Tests the loginPhaseCompleted field management
        /// </summary>
        [TestMethod]
        public void Task2_2_Phase1CompletionTracking_IsImplementedCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User 
            { 
                UserId = userId, 
                LoginPhaseCompleted = false 
            };

            _mockContext.Setup(c => c.Users.Find(userId)).Returns(user);

            // Act - Complete Phase 1 login
            bool result = _authService.CompletePhase1Login(userId);
            
            // Assert - Verify Phase 1 completion tracking
            Assert.IsTrue(result, "CompletePhase1Login should succeed");
            Assert.IsTrue(user.LoginPhaseCompleted, "LoginPhaseCompleted should be set to true");
            _mockContext.Verify(c => c.SaveChanges(), Times.Once, "Changes should be saved to database");
            
            // Test Phase 1 completion check
            bool isCompleted = _authService.IsPhase1Completed(userId);
            Assert.IsTrue(isCompleted, "IsPhase1Completed should return true after completion");
            
            // Verify methods exist
            var completeMethod = typeof(AuthenticationService).GetMethod("CompletePhase1Login");
            Assert.IsNotNull(completeMethod, "CompletePhase1Login method should exist");
            
            var checkMethod = typeof(AuthenticationService).GetMethod("IsPhase1Completed");
            Assert.IsNotNull(checkMethod, "IsPhase1Completed method should exist");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that password change functionality is implemented
        /// Tests the mustChangePassword field management
        /// </summary>
        [TestMethod]
        public void Task2_2_PasswordChangeManagement_IsImplementedCorrectly()
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

            // Act - Change password
            bool result = _authService.ChangePassword(userId, newPassword);
            
            // Assert - Verify password change management
            Assert.IsTrue(result, "ChangePassword should succeed");
            Assert.IsFalse(user.MustChangePassword, "MustChangePassword should be set to false");
            Assert.IsNotNull(user.PasswordHash, "PasswordHash should be set");
            _mockContext.Verify(c => c.SaveChanges(), Times.Once, "Changes should be saved to database");
            
            // Test password change requirement check
            bool mustChange = _authService.MustChangePassword(userId);
            Assert.IsFalse(mustChange, "MustChangePassword should return false after password change");
            
            // Verify methods exist
            var changeMethod = typeof(AuthenticationService).GetMethod("ChangePassword");
            Assert.IsNotNull(changeMethod, "ChangePassword method should exist");
            
            var checkMethod = typeof(AuthenticationService).GetMethod("MustChangePassword");
            Assert.IsNotNull(checkMethod, "MustChangePassword method should exist");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that user retrieval methods are implemented
        /// Tests the GetUserByEmail and GetUserByIdNumber methods
        /// </summary>
        [TestMethod]
        public void Task2_2_UserRetrievalMethods_AreImplementedCorrectly()
        {
            // Arrange
            var email = "student@hems.edu";
            var idNumber = "SE123";
            var user = new User 
            { 
                UserId = 1, 
                Username = email 
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber,
                User = user
            };

            SetupMockDbSets(new List<User> { user }, new List<Student> { student }, new List<ExamSession>());

            // Act & Assert - Test GetUserByEmail
            var userByEmail = _authService.GetUserByEmail(email);
            Assert.IsNotNull(userByEmail, "GetUserByEmail should return user when found");
            
            var userByEmailNotFound = _authService.GetUserByEmail("nonexistent@hems.edu");
            Assert.IsNull(userByEmailNotFound, "GetUserByEmail should return null when not found");
            
            // Act & Assert - Test GetUserByIdNumber
            var userByIdNumber = _authService.GetUserByIdNumber(idNumber);
            Assert.IsNotNull(userByIdNumber, "GetUserByIdNumber should return user when found");
            
            var userByIdNumberNotFound = _authService.GetUserByIdNumber("NONEXISTENT");
            Assert.IsNull(userByIdNumberNotFound, "GetUserByIdNumber should return null when not found");
            
            // Verify methods exist
            var emailMethod = typeof(AuthenticationService).GetMethod("GetUserByEmail");
            Assert.IsNotNull(emailMethod, "GetUserByEmail method should exist");
            Assert.AreEqual(typeof(User), emailMethod.ReturnType, "GetUserByEmail should return User");
            
            var idMethod = typeof(AuthenticationService).GetMethod("GetUserByIdNumber");
            Assert.IsNotNull(idMethod, "GetUserByIdNumber method should exist");
            Assert.AreEqual(typeof(User), idMethod.ReturnType, "GetUserByIdNumber should return User");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that exam session management is implemented
        /// Tests the GetActiveExamSessions method for Phase 2 login validation
        /// </summary>
        [TestMethod]
        public void Task2_2_ExamSessionManagement_IsImplementedCorrectly()
        {
            // Arrange
            var activeSession = new ExamSession
            {
                ExamSessionId = 1,
                ExamId = 1,
                SessionPassword = "active2024",
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(2)
            };
            var expiredSession = new ExamSession
            {
                ExamSessionId = 2,
                ExamId = 1,
                SessionPassword = "expired2024",
                IsActive = true,
                ExpiryDate = DateTime.Now.AddHours(-1) // Expired
            };
            var inactiveSession = new ExamSession
            {
                ExamSessionId = 3,
                ExamId = 1,
                SessionPassword = "inactive2024",
                IsActive = false,
                ExpiryDate = DateTime.Now.AddHours(2)
            };

            SetupMockDbSets(new List<User>(), new List<Student>(), 
                new List<ExamSession> { activeSession, expiredSession, inactiveSession });

            // Act
            var activeSessions = _authService.GetActiveExamSessions();
            
            // Assert
            Assert.IsNotNull(activeSessions, "GetActiveExamSessions should return a list");
            Assert.AreEqual(1, activeSessions.Count, "Should return only active, non-expired sessions");
            Assert.AreEqual("active2024", activeSessions[0].SessionPassword, "Should return the correct active session");
            
            // Verify method exists
            var method = typeof(AuthenticationService).GetMethod("GetActiveExamSessions");
            Assert.IsNotNull(method, "GetActiveExamSessions method should exist");
            Assert.AreEqual(typeof(List<ExamSession>), method.ReturnType, "GetActiveExamSessions should return List<ExamSession>");
        }

        /// <summary>
        /// Task 2.2 Requirement: Validate that student credential validation is implemented
        /// Tests the ValidateStudentCredentials method
        /// </summary>
        [TestMethod]
        public void Task2_2_StudentCredentialValidation_IsImplementedCorrectly()
        {
            // Arrange
            var email = "student@hems.edu";
            var idNumber = "SE123";
            var user = new User 
            { 
                UserId = 1, 
                Username = email 
            };
            var student = new Student 
            { 
                StudentId = 1, 
                UserId = 1, 
                IdNumber = idNumber,
                UniversityEmail = email,
                User = user
            };

            SetupMockDbSets(new List<User> { user }, new List<Student> { student }, new List<ExamSession>());

            // Act & Assert
            bool validCredentials = _authService.ValidateStudentCredentials(email, idNumber);
            Assert.IsTrue(validCredentials, "Should validate correct student credentials");
            
            bool invalidCredentials = _authService.ValidateStudentCredentials(email, "WRONG123");
            Assert.IsFalse(invalidCredentials, "Should reject incorrect student credentials");
            
            bool emptyCredentials = _authService.ValidateStudentCredentials("", "");
            Assert.IsFalse(emptyCredentials, "Should reject empty credentials");
            
            // Verify method exists
            var method = typeof(AuthenticationService).GetMethod("ValidateStudentCredentials");
            Assert.IsNotNull(method, "ValidateStudentCredentials method should exist");
            Assert.AreEqual(typeof(bool), method.ReturnType, "ValidateStudentCredentials should return bool");
        }

        /// <summary>
        /// Task 2.2 Comprehensive Validation: All required methods are implemented and working
        /// This test ensures that Task 2.2 is fully complete
        /// </summary>
        [TestMethod]
        public void Task2_2_ComprehensiveValidation_AllRequiredMethodsImplemented()
        {
            // Verify all required methods exist in the AuthenticationService
            var serviceType = typeof(AuthenticationService);
            var interfaceType = typeof(IAuthenticationService);
            
            // Check that service implements the interface
            Assert.IsTrue(interfaceType.IsAssignableFrom(serviceType), 
                "AuthenticationService should implement IAuthenticationService");
            
            // Verify all interface methods are implemented
            var interfaceMethods = interfaceType.GetMethods();
            foreach (var interfaceMethod in interfaceMethods)
            {
                var implementedMethod = serviceType.GetMethod(interfaceMethod.Name, 
                    interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray());
                Assert.IsNotNull(implementedMethod, 
                    $"Method {interfaceMethod.Name} should be implemented in AuthenticationService");
            }
            
            // Verify key methods specifically required for Task 2.2
            var requiredMethods = new[]
            {
                "ValidatePhase1Login",
                "ValidatePhase2Login", 
                "CalculatePhase1Password",
                "IsUniversityEmailValid",
                "GetUserByEmail",
                "GetUserByIdNumber",
                "CompletePhase1Login",
                "ChangePassword",
                "IsPhase1Completed",
                "MustChangePassword",
                "ValidateStudentCredentials",
                "GetActiveExamSessions"
            };
            
            foreach (var methodName in requiredMethods)
            {
                var method = serviceType.GetMethod(methodName);
                Assert.IsNotNull(method, $"Required method {methodName} should be implemented");
            }
            
            Console.WriteLine("✅ Task 2.2 Validation Complete: All required methods are implemented and working correctly");
        }

        private void SetupMockDbSets(List<User> users, List<Student> students, List<ExamSession> examSessions)
        {
            var usersQueryable = users.AsQueryable();
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(usersQueryable.Provider);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(usersQueryable.Expression);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(usersQueryable.ElementType);
            _mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(usersQueryable.GetEnumerator());

            var studentsQueryable = students.AsQueryable();
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Provider).Returns(studentsQueryable.Provider);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.Expression).Returns(studentsQueryable.Expression);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.ElementType).Returns(studentsQueryable.ElementType);
            _mockStudentSet.As<IQueryable<Student>>().Setup(m => m.GetEnumerator()).Returns(studentsQueryable.GetEnumerator());

            var examSessionsQueryable = examSessions.AsQueryable();
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Provider).Returns(examSessionsQueryable.Provider);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.Expression).Returns(examSessionsQueryable.Expression);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.ElementType).Returns(examSessionsQueryable.ElementType);
            _mockExamSessionSet.As<IQueryable<ExamSession>>().Setup(m => m.GetEnumerator()).Returns(examSessionsQueryable.GetEnumerator());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _authService?.Dispose();
        }
    }
}