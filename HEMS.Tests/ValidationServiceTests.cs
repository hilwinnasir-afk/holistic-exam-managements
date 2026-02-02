using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using HEMS.Services;
using HEMS.Models;
using HEMS.Models.ViewModels;

namespace HEMS.Tests
{
    /// <summary>
    /// Tests for comprehensive validation service
    /// </summary>
    [TestClass]
    public class ValidationServiceTests
    {
        private ValidationService _validationService;
        private Mock<HEMSContext> _mockContext;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _validationService = new ValidationService(_mockContext.Object);
        }

        #region Student Import Validation Tests

        [TestMethod]
        public void ValidateStudentImport_ValidModel_ReturnsSuccess()
        {
            // Arrange
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("students.csv");

            var model = new StudentImportViewModel
            {
                ImportFile = mockFile.Object,
                BatchYear = 2024,
                SkipHeaderRow = true
            };

            // Act
            var result = _validationService.ValidateStudentImport(model);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid student import model should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid model should have no errors");
        }

        [TestMethod]
        public void ValidateStudentImport_InvalidBatchYear_ReturnsError()
        {
            // Arrange
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("students.csv");

            var model = new StudentImportViewModel
            {
                ImportFile = mockFile.Object,
                BatchYear = 1999, // Invalid year
                SkipHeaderRow = true
            };

            // Act
            var result = _validationService.ValidateStudentImport(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid batch year should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "BatchYear"), "Should have batch year error");
        }

        [TestMethod]
        public void ValidateStudentImport_NoFile_ReturnsError()
        {
            // Arrange
            var model = new StudentImportViewModel
            {
                ImportFile = null,
                BatchYear = 2024,
                SkipHeaderRow = true
            };

            // Act
            var result = _validationService.ValidateStudentImport(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Missing file should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "ImportFile"), "Should have file error");
        }

        [TestMethod]
        public void ValidateStudentImport_FutureBatchYear_ReturnsWarning()
        {
            // Arrange
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("students.csv");

            var model = new StudentImportViewModel
            {
                ImportFile = mockFile.Object,
                BatchYear = DateTime.Now.Year + 10, // Far future
                SkipHeaderRow = true
            };

            // Act
            var result = _validationService.ValidateStudentImport(model);

            // Assert
            Assert.IsTrue(result.Errors.Any(e => e.Severity == ValidationSeverity.Warning), 
                "Future batch year should generate warning");
        }

        #endregion

        #region Student Data Validation Tests

        [TestMethod]
        public void ValidateStudentData_ValidStudent_ReturnsSuccess()
        {
            // Arrange
            var student = new StudentImportModel
            {
                IdNumber = "SE123",
                UniversityEmail = "student@hems.edu",
                BatchYear = 2024
            };

            // Act
            var result = _validationService.ValidateStudentData(student);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid student data should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid student should have no errors");
        }

        [TestMethod]
        public void ValidateStudentData_InvalidIdNumber_ReturnsError()
        {
            // Arrange
            var student = new StudentImportModel
            {
                IdNumber = "SE@123", // Invalid characters
                UniversityEmail = "student@hems.edu",
                BatchYear = 2024
            };

            // Act
            var result = _validationService.ValidateStudentData(student);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid ID number should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("ID Number")), 
                "Should have ID number validation error");
        }

        [TestMethod]
        public void ValidateStudentData_InvalidEmail_ReturnsError()
        {
            // Arrange
            var student = new StudentImportModel
            {
                IdNumber = "SE123",
                UniversityEmail = "student@gmail.com", // Non-university domain
                BatchYear = 2024
            };

            // Act
            var result = _validationService.ValidateStudentData(student);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid university email should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("university email")), 
                "Should have university email validation error");
        }

        #endregion

        #region Exam Creation Validation Tests

        [TestMethod]
        public void ValidateExamCreation_ValidExam_ReturnsSuccess()
        {
            // Arrange
            var model = new ExamCreateViewModel
            {
                Title = "Software Engineering Final Exam",
                AcademicYear = 2024,
                DurationMinutes = 120
            };

            // Act
            var result = _validationService.ValidateExamCreation(model);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid exam creation model should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid exam should have no errors");
        }

        [TestMethod]
        public void ValidateExamCreation_ShortTitle_ReturnsError()
        {
            // Arrange
            var model = new ExamCreateViewModel
            {
                Title = "SE", // Too short
                AcademicYear = 2024,
                DurationMinutes = 120
            };

            // Act
            var result = _validationService.ValidateExamCreation(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Short title should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "Title"), "Should have title error");
        }

        [TestMethod]
        public void ValidateExamCreation_InvalidDuration_ReturnsError()
        {
            // Arrange
            var model = new ExamCreateViewModel
            {
                Title = "Software Engineering Final Exam",
                AcademicYear = 2024,
                DurationMinutes = 15 // Too short
            };

            // Act
            var result = _validationService.ValidateExamCreation(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid duration should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "DurationMinutes"), "Should have duration error");
        }

        [TestMethod]
        public void ValidateExamCreation_LongDuration_ReturnsWarning()
        {
            // Arrange
            var model = new ExamCreateViewModel
            {
                Title = "Software Engineering Final Exam",
                AcademicYear = 2024,
                DurationMinutes = 300 // 5 hours - very long
            };

            // Act
            var result = _validationService.ValidateExamCreation(model);

            // Assert
            Assert.IsTrue(result.Errors.Any(e => e.Severity == ValidationSeverity.Warning), 
                "Long duration should generate warning");
        }

        #endregion

        #region Question Creation Validation Tests

        [TestMethod]
        public void ValidateQuestionCreation_ValidQuestion_ReturnsSuccess()
        {
            // Arrange
            var model = new QuestionCreateViewModel
            {
                ExamId = 1,
                QuestionText = "What is the primary purpose of software engineering?",
                Choice1 = "To write code quickly",
                Choice2 = "To create maintainable software systems",
                Choice3 = "To use the latest technologies",
                Choice4 = "To minimize development costs",
                CorrectChoiceIndex = 1
            };

            // Act
            var result = _validationService.ValidateQuestionCreation(model);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid question creation model should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid question should have no errors");
        }

        [TestMethod]
        public void ValidateQuestionCreation_ShortQuestionText_ReturnsError()
        {
            // Arrange
            var model = new QuestionCreateViewModel
            {
                ExamId = 1,
                QuestionText = "What?", // Too short
                Choice1 = "Option A",
                Choice2 = "Option B",
                CorrectChoiceIndex = 0
            };

            // Act
            var result = _validationService.ValidateQuestionCreation(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Short question text should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "QuestionText"), "Should have question text error");
        }

        [TestMethod]
        public void ValidateQuestionCreation_InsufficientChoices_ReturnsError()
        {
            // Arrange
            var model = new QuestionCreateViewModel
            {
                ExamId = 1,
                QuestionText = "What is the primary purpose of software engineering?",
                Choice1 = "Option A",
                Choice2 = "", // Missing choice
                CorrectChoiceIndex = 0
            };

            // Act
            var result = _validationService.ValidateQuestionCreation(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Insufficient choices should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "Choices"), "Should have choices error");
        }

        [TestMethod]
        public void ValidateQuestionCreation_DuplicateChoices_ReturnsError()
        {
            // Arrange
            var model = new QuestionCreateViewModel
            {
                ExamId = 1,
                QuestionText = "What is the primary purpose of software engineering?",
                Choice1 = "Same option",
                Choice2 = "Same option", // Duplicate
                Choice3 = "Different option",
                CorrectChoiceIndex = 0
            };

            // Act
            var result = _validationService.ValidateQuestionCreation(model);

            // Assert
            Assert.IsFalse(result.IsValid, "Duplicate choices should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Duplicate choices")), 
                "Should have duplicate choices error");
        }

        #endregion

        #region Authentication Data Validation Tests

        [TestMethod]
        public void ValidateAuthenticationData_ValidData_ReturnsSuccess()
        {
            // Arrange
            var email = "student@hems.edu";
            var password = "StrongPass123!";

            // Act
            var result = _validationService.ValidateAuthenticationData(email, password);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid authentication data should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid data should have no errors");
        }

        [TestMethod]
        public void ValidateAuthenticationData_InvalidEmail_ReturnsError()
        {
            // Arrange
            var email = "invalid-email";
            var password = "StrongPass123!";

            // Act
            var result = _validationService.ValidateAuthenticationData(email, password);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid email should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "Email"), "Should have email error");
        }

        [TestMethod]
        public void ValidateAuthenticationData_WeakPassword_ReturnsError()
        {
            // Arrange
            var email = "student@hems.edu";
            var password = "weak";

            // Act
            var result = _validationService.ValidateAuthenticationData(email, password);

            // Assert
            Assert.IsFalse(result.IsValid, "Weak password should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "Password"), "Should have password error");
        }

        [TestMethod]
        public void ValidateAuthenticationData_PasswordMismatch_ReturnsError()
        {
            // Arrange
            var email = "student@hems.edu";
            var password = "StrongPass123!";
            var confirmPassword = "DifferentPass123!";

            // Act
            var result = _validationService.ValidateAuthenticationData(email, password, confirmPassword);

            // Assert
            Assert.IsFalse(result.IsValid, "Password mismatch should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Field == "ConfirmPassword"), "Should have confirm password error");
        }

        #endregion

        #region Session Password Validation Tests

        [TestMethod]
        public void ValidateSessionPassword_ValidPassword_ReturnsSuccess()
        {
            // Arrange
            var password = "exam2024";

            // Act
            var result = _validationService.ValidateSessionPassword(password);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid session password should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid password should have no errors");
        }

        [TestMethod]
        public void ValidateSessionPassword_TooShort_ReturnsError()
        {
            // Arrange
            var password = "abc"; // Too short

            // Act
            var result = _validationService.ValidateSessionPassword(password);

            // Assert
            Assert.IsFalse(result.IsValid, "Short password should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("6 characters")), 
                "Should have length error");
        }

        [TestMethod]
        public void ValidateSessionPassword_WeakPassword_ReturnsWarning()
        {
            // Arrange
            var password = "password123"; // Contains weak pattern

            // Act
            var result = _validationService.ValidateSessionPassword(password);

            // Assert
            Assert.IsTrue(result.Errors.Any(e => e.Severity == ValidationSeverity.Warning), 
                "Weak password should generate warning");
        }

        #endregion

        #region File Upload Validation Tests

        [TestMethod]
        public void ValidateFileUpload_ValidFile_ReturnsSuccess()
        {
            // Arrange
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("students.csv");

            var allowedExtensions = new[] { ".csv", ".txt" };
            var maxSize = 10 * 1024 * 1024; // 10MB

            // Act
            var result = _validationService.ValidateFileUpload(mockFile.Object, allowedExtensions, maxSize);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid file should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid file should have no errors");
        }

        [TestMethod]
        public void ValidateFileUpload_InvalidExtension_ReturnsError()
        {
            // Arrange
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("students.pdf"); // Invalid extension

            var allowedExtensions = new[] { ".csv", ".txt" };
            var maxSize = 10 * 1024 * 1024;

            // Act
            var result = _validationService.ValidateFileUpload(mockFile.Object, allowedExtensions, maxSize);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid file extension should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("not allowed")), 
                "Should have file type error");
        }

        [TestMethod]
        public void ValidateFileUpload_TooLarge_ReturnsError()
        {
            // Arrange
            var mockFile = new Mock<HttpPostedFileBase>();
            mockFile.Setup(f => f.ContentLength).Returns(20 * 1024 * 1024); // 20MB
            mockFile.Setup(f => f.FileName).Returns("students.csv");

            var allowedExtensions = new[] { ".csv", ".txt" };
            var maxSize = 10 * 1024 * 1024; // 10MB limit

            // Act
            var result = _validationService.ValidateFileUpload(mockFile.Object, allowedExtensions, maxSize);

            // Assert
            Assert.IsFalse(result.IsValid, "Large file should fail validation");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("exceeds")), 
                "Should have file size error");
        }

        #endregion

        #region Model Validation Tests

        [TestMethod]
        public void ValidateModel_ValidModel_ReturnsSuccess()
        {
            // Arrange
            var student = new Student
            {
                IdNumber = "SE123",
                UniversityEmail = "student@hems.edu",
                BatchYear = 2024
            };

            // Act
            var result = _validationService.ValidateModel(student);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid model should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Valid model should have no errors");
        }

        [TestMethod]
        public void ValidateModel_InvalidModel_ReturnsErrors()
        {
            // Arrange
            var student = new Student
            {
                IdNumber = "", // Required field missing
                UniversityEmail = "invalid-email", // Invalid format
                BatchYear = 1999 // Invalid range
            };

            // Act
            var result = _validationService.ValidateModel(student);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid model should fail validation");
            Assert.IsTrue(result.Errors.Count > 0, "Invalid model should have errors");
        }

        #endregion

        [TestCleanup]
        public void Cleanup()
        {
            _validationService?.Dispose();
        }
    }
}