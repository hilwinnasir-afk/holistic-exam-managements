using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HEMS.Controllers;
using HEMS.Models;
using HEMS.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HEMS.Tests
{
    [TestClass]
    public class ExamErrorHandlingTests
    {
        private Mock<HEMSContext> _mockContext;
        private Mock<IExamService> _mockExamService;
        private Mock<ITimerService> _mockTimerService;
        private Mock<IGradingService> _mockGradingService;
        private Mock<IAuditService> _mockAuditService;
        private Mock<IDataIntegrityService> _mockDataIntegrityService;
        private ExamController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _mockExamService = new Mock<IExamService>();
            _mockTimerService = new Mock<ITimerService>();
            _mockGradingService = new Mock<IGradingService>();
            _mockAuditService = new Mock<IAuditService>();
            _mockDataIntegrityService = new Mock<IDataIntegrityService>();

            _controller = new ExamController(
                _mockContext.Object,
                _mockExamService.Object,
                _mockTimerService.Object,
                _mockGradingService.Object,
                _mockAuditService.Object,
                _mockDataIntegrityService.Object
            );

            // Setup controller context for session
            var mockControllerContext = new Mock<ControllerContext>();
            var mockSession = new Mock<System.Web.SessionState.HttpSessionStateBase>();
            mockControllerContext.Setup(c => c.HttpContext.Session).Returns(mockSession.Object);
            _controller.ControllerContext = mockControllerContext.Object;
        }

        [TestMethod]
        public void StartExam_ExamNotFound_ReturnsExamErrorView()
        {
            // Arrange
            int examId = 999;
            int studentId = 1;

            _controller.Session["CurrentStudentId"] = studentId;

            var integrityResult = new DataIntegrityValidationResult { IsValid = true };
            _mockDataIntegrityService.Setup(x => x.ValidateExamIntegrity(examId))
                .Returns(integrityResult);

            var enhancedValidation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.ExamNotFound,
                $"Exam with ID {examId} not found"
            );
            _mockExamService.Setup(x => x.ValidateExamAccessEnhanced(examId, studentId))
                .Returns(enhancedValidation);

            // Act
            var result = _controller.StartExam(examId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ExamError", result.ViewName);
            Assert.IsInstanceOfType(result.Model, typeof(ExamAccessError));
            
            var error = result.Model as ExamAccessError;
            Assert.AreEqual(ExamAccessErrorType.ExamNotFound, error.ErrorType);
            Assert.AreEqual("The requested exam could not be found.", error.UserFriendlyMessage);
            Assert.IsTrue(error.ShowContactSupport);
        }

        [TestMethod]
        public void StartExam_ExamNotPublished_ReturnsExamErrorView()
        {
            // Arrange
            int examId = 1;
            int studentId = 1;

            _controller.Session["CurrentStudentId"] = studentId;

            var integrityResult = new DataIntegrityValidationResult { IsValid = true };
            _mockDataIntegrityService.Setup(x => x.ValidateExamIntegrity(examId))
                .Returns(integrityResult);

            var enhancedValidation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.ExamNotPublished,
                "Exam is not published"
            );
            _mockExamService.Setup(x => x.ValidateExamAccessEnhanced(examId, studentId))
                .Returns(enhancedValidation);

            // Act
            var result = _controller.StartExam(examId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ExamError", result.ViewName);
            
            var error = result.Model as ExamAccessError;
            Assert.AreEqual(ExamAccessErrorType.ExamNotPublished, error.ErrorType);
            Assert.AreEqual("This exam is not yet available for students.", error.UserFriendlyMessage);
            Assert.IsTrue(error.ShowContactSupport);
        }

        [TestMethod]
        public void StartExam_StudentNotEligible_ReturnsExamErrorView()
        {
            // Arrange
            int examId = 1;
            int studentId = 1;

            _controller.Session["CurrentStudentId"] = studentId;

            var integrityResult = new DataIntegrityValidationResult { IsValid = true };
            _mockDataIntegrityService.Setup(x => x.ValidateExamIntegrity(examId))
                .Returns(integrityResult);

            var enhancedValidation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.StudentNotEligible,
                "Student has not completed Phase 1 identity verification",
                redirectUrl: "/Authentication/Phase1Login"
            );
            _mockExamService.Setup(x => x.ValidateExamAccessEnhanced(examId, studentId))
                .Returns(enhancedValidation);

            // Act
            var result = _controller.StartExam(examId) as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("/Authentication/Phase1Login", result.Url);
        }

        [TestMethod]
        public void StartExam_ExamAlreadyCompleted_ReturnsExamErrorView()
        {
            // Arrange
            int examId = 1;
            int studentId = 1;

            _controller.Session["CurrentStudentId"] = studentId;

            var integrityResult = new DataIntegrityValidationResult { IsValid = true };
            _mockDataIntegrityService.Setup(x => x.ValidateExamIntegrity(examId))
                .Returns(integrityResult);

            var enhancedValidation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.ExamAlreadyCompleted,
                "Student has already submitted this exam",
                redirectUrl: "/Exam/Results/123"
            );
            _mockExamService.Setup(x => x.ValidateExamAccessEnhanced(examId, studentId))
                .Returns(enhancedValidation);

            // Act
            var result = _controller.StartExam(examId) as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("/Exam/Results/123", result.Url);
        }

        [TestMethod]
        public void StartExam_DataIntegrityViolation_ReturnsExamErrorView()
        {
            // Arrange
            int examId = 1;
            int studentId = 1;

            _controller.Session["CurrentStudentId"] = studentId;

            var integrityResult = new DataIntegrityValidationResult 
            { 
                IsValid = false,
                Errors = new List<string> { "Exam data corrupted" }
            };
            _mockDataIntegrityService.Setup(x => x.ValidateExamIntegrity(examId))
                .Returns(integrityResult);

            // Act
            var result = _controller.StartExam(examId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ExamError", result.ViewName);
            
            var error = result.Model as ExamAccessError;
            Assert.AreEqual(ExamAccessErrorType.DataIntegrityViolation, error.ErrorType);
            Assert.IsTrue(error.ShowContactSupport);
        }

        [TestMethod]
        public void SaveAnswer_SessionExpired_ReturnsJsonErrorWithRedirect()
        {
            // Arrange
            int questionId = 1;
            int choiceId = 1;

            // No session data to simulate expired session
            _controller.Session["CurrentStudentExamId"] = null;

            // Act
            var result = _controller.SaveAnswer(questionId, choiceId) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.AreEqual("Your exam session has expired. Please log in again.", data.message);
            Assert.AreEqual("SessionExpired", data.errorType);
            Assert.IsNotNull(data.redirect);
        }

        [TestMethod]
        public void SaveAnswer_ExamAlreadySubmitted_ReturnsJsonErrorWithRedirect()
        {
            // Arrange
            int questionId = 1;
            int choiceId = 1;
            int studentExamId = 1;

            _controller.Session["CurrentStudentExamId"] = studentExamId;

            var validation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.ExamAlreadyCompleted,
                "Cannot modify answers after exam submission",
                redirectUrl: $"/Exam/SubmissionConfirmation/{studentExamId}"
            );
            _mockExamService.Setup(x => x.ValidateAnswerSubmissionEnhanced(studentExamId, questionId, choiceId))
                .Returns(validation);

            // Act
            var result = _controller.SaveAnswer(questionId, choiceId) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.IsNotNull(data.redirect);
        }

        [TestMethod]
        public void SaveAnswer_TimerExpired_ReturnsJsonErrorWithRedirect()
        {
            // Arrange
            int questionId = 1;
            int choiceId = 1;
            int studentExamId = 1;

            _controller.Session["CurrentStudentExamId"] = studentExamId;

            var validation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.TimerExpired,
                "Exam time has expired",
                redirectUrl: $"/Exam/SubmissionConfirmation/{studentExamId}"
            );
            _mockExamService.Setup(x => x.ValidateAnswerSubmissionEnhanced(studentExamId, questionId, choiceId))
                .Returns(validation);

            // Act
            var result = _controller.SaveAnswer(questionId, choiceId) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.IsNotNull(data.redirect);
        }

        [TestMethod]
        public void SaveAnswer_InvalidQuestion_ReturnsJsonError()
        {
            // Arrange
            int questionId = 999;
            int choiceId = 1;
            int studentExamId = 1;

            _controller.Session["CurrentStudentExamId"] = studentExamId;

            var validation = EnhancedExamValidationResult.Failure(
                ExamAccessErrorType.InvalidQuestionNavigation,
                "Question does not belong to this exam"
            );
            _mockExamService.Setup(x => x.ValidateAnswerSubmissionEnhanced(studentExamId, questionId, choiceId))
                .Returns(validation);

            // Act
            var result = _controller.SaveAnswer(questionId, choiceId) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Data;
            Assert.IsFalse(data.success);
            Assert.AreEqual("Question does not belong to this exam", data.message);
            Assert.AreEqual("InvalidQuestionNavigation", data.errorType);
        }

        [TestMethod]
        public void ExamAccessError_Create_SetsCorrectUserFriendlyMessages()
        {
            // Test various error types
            var testCases = new[]
            {
                new { ErrorType = ExamAccessErrorType.ExamNotFound, ExpectedMessage = "The requested exam could not be found." },
                new { ErrorType = ExamAccessErrorType.ExamNotPublished, ExpectedMessage = "This exam is not yet available for students." },
                new { ErrorType = ExamAccessErrorType.StudentNotEligible, ExpectedMessage = "You are not eligible to take this exam." },
                new { ErrorType = ExamAccessErrorType.ExamAlreadyCompleted, ExpectedMessage = "You have already completed and submitted this exam." },
                new { ErrorType = ExamAccessErrorType.InvalidSessionPassword, ExpectedMessage = "The exam session password is incorrect." },
                new { ErrorType = ExamAccessErrorType.TimerExpired, ExpectedMessage = "The exam time limit has been reached." },
                new { ErrorType = ExamAccessErrorType.DatabaseConnectivityError, ExpectedMessage = "The system is temporarily experiencing technical difficulties." }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var error = ExamAccessError.Create(testCase.ErrorType);

                // Assert
                Assert.AreEqual(testCase.ExpectedMessage, error.UserFriendlyMessage, 
                    $"Error type {testCase.ErrorType} should have correct user-friendly message");
                Assert.IsNotNull(error.SuggestedAction, 
                    $"Error type {testCase.ErrorType} should have suggested action");
            }
        }

        [TestMethod]
        public void ExamAccessError_Create_SetsRetryAndSupportFlags()
        {
            // Test errors that should show retry option
            var retryErrors = new[]
            {
                ExamAccessErrorType.InvalidSessionPassword,
                ExamAccessErrorType.NetworkConnectivityIssue,
                ExamAccessErrorType.DatabaseConnectivityError,
                ExamAccessErrorType.ServiceUnavailable
            };

            foreach (var errorType in retryErrors)
            {
                var error = ExamAccessError.Create(errorType);
                Assert.IsTrue(error.ShowRetryOption, 
                    $"Error type {errorType} should show retry option");
            }

            // Test errors that should show contact support
            var supportErrors = new[]
            {
                ExamAccessErrorType.ExamNotFound,
                ExamAccessErrorType.DataIntegrityViolation,
                ExamAccessErrorType.UnauthorizedAccess
            };

            foreach (var errorType in supportErrors)
            {
                var error = ExamAccessError.Create(errorType);
                Assert.IsTrue(error.ShowContactSupport, 
                    $"Error type {errorType} should show contact support");
            }
        }

        [TestMethod]
        public void EnhancedExamValidationResult_Success_ReturnsValidResult()
        {
            // Act
            var result = EnhancedExamValidationResult.Success();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.Error);
        }

        [TestMethod]
        public void EnhancedExamValidationResult_Failure_ReturnsInvalidResultWithError()
        {
            // Arrange
            var errorType = ExamAccessErrorType.ExamNotFound;
            var technicalMessage = "Test technical message";
            var redirectUrl = "/test/redirect";

            // Act
            var result = EnhancedExamValidationResult.Failure(errorType, technicalMessage, redirectUrl);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(errorType, result.Error.ErrorType);
            Assert.AreEqual(technicalMessage, result.Error.TechnicalMessage);
            Assert.AreEqual(redirectUrl, result.RedirectUrl);
        }
    }
}