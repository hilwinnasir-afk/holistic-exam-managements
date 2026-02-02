using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Controllers;
using HEMS.Models;
using HEMS.Services;

namespace HEMS.Tests
{
    /// <summary>
    /// Tests for the error handling system to ensure all error pages are properly configured
    /// </summary>
    [TestClass]
    public class ErrorHandlingSystemTests
    {
        private ErrorController _errorController;
        private ErrorHandlingService _errorHandlingService;

        [TestInitialize]
        public void Setup()
        {
            _errorController = new ErrorController();
            _errorHandlingService = new ErrorHandlingService();
        }

        [TestMethod]
        public void ErrorController_NotFound_ReturnsNotFoundView()
        {
            // Act
            var result = _errorController.NotFound() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotFound", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_AuthenticationFailure_ReturnsAuthenticationFailureView()
        {
            // Act
            var result = _errorController.AuthenticationFailure() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("AuthenticationFailure", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_NetworkError_ReturnsNetworkErrorView()
        {
            // Act
            var result = _errorController.NetworkError() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NetworkError", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_DatabaseError_ReturnsDatabaseErrorView()
        {
            // Act
            var result = _errorController.DatabaseError() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("DatabaseError", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_ExamSystemFailure_ReturnsExamSystemFailureView()
        {
            // Act
            var result = _errorController.ExamSystemFailure() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ExamSystemFailure", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_TimerSystemFailure_ReturnsTimerSystemFailureView()
        {
            // Act
            var result = _errorController.TimerSystemFailure() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("TimerSystemFailure", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_SubmissionFailure_ReturnsSubmissionFailureView()
        {
            // Act
            var result = _errorController.SubmissionFailure() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SubmissionFailure", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_GradingSystemFailure_ReturnsGradingSystemFailureView()
        {
            // Act
            var result = _errorController.GradingSystemFailure() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("GradingSystemFailure", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_MaintenanceMode_ReturnsMaintenanceModeView()
        {
            // Act
            var result = _errorController.MaintenanceMode() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("MaintenanceMode", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_SessionTimeout_ReturnsSessionTimeoutView()
        {
            // Act
            var result = _errorController.SessionTimeout() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SessionTimeout", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_ServiceUnavailable_ReturnsServiceUnavailableView()
        {
            // Act
            var result = _errorController.ServiceUnavailable() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ServiceUnavailable", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_RequestTimeout_ReturnsRequestTimeoutView()
        {
            // Act
            var result = _errorController.RequestTimeout() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("RequestTimeout", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_InternalServerError_ReturnsInternalServerErrorView()
        {
            // Act
            var result = _errorController.InternalServerError() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("InternalServerError", result.ViewName);
        }

        [TestMethod]
        public void ErrorController_AccessDenied_ReturnsAccessDeniedView()
        {
            // Act
            var result = _errorController.AccessDenied() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("AccessDenied", result.ViewName);
        }

        [TestMethod]
        public void ErrorHandlingService_GetErrorActionResult_DatabaseException_ReturnsDatabaseError()
        {
            // Arrange
            var exception = new System.Data.SqlClient.SqlException();

            // Act
            var result = _errorHandlingService.GetErrorActionResult(exception) as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Url.Contains("DatabaseError"));
        }

        [TestMethod]
        public void ErrorHandlingService_GetErrorActionResult_TimeoutException_ReturnsRequestTimeout()
        {
            // Arrange
            var exception = new TimeoutException();

            // Act
            var result = _errorHandlingService.GetErrorActionResult(exception) as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Url.Contains("RequestTimeout"));
        }

        [TestMethod]
        public void ErrorHandlingService_GetErrorActionResult_ExamTimerException_ReturnsTimerSystemFailure()
        {
            // Arrange
            var exception = new Exception("Timer system failure occurred");

            // Act
            var result = _errorHandlingService.GetErrorActionResult(exception, "Exam") as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Url.Contains("TimerSystemFailure"));
        }

        [TestMethod]
        public void ErrorHandlingService_GetErrorActionResult_ExamSubmissionException_ReturnsSubmissionFailure()
        {
            // Arrange
            var exception = new Exception("Submission failed");

            // Act
            var result = _errorHandlingService.GetErrorActionResult(exception, "Exam") as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Url.Contains("SubmissionFailure"));
        }

        [TestMethod]
        public void ErrorHandlingService_GetErrorActionResult_ExamGradingException_ReturnsGradingSystemFailure()
        {
            // Arrange
            var exception = new Exception("Grading system error");

            // Act
            var result = _errorHandlingService.GetErrorActionResult(exception, "Exam") as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Url.Contains("GradingSystemFailure"));
        }

        [TestMethod]
        public void ErrorHandlingService_GetErrorActionResult_AuthenticationException_ReturnsAuthenticationFailure()
        {
            // Arrange
            var exception = new Exception("Authentication failed");

            // Act
            var result = _errorHandlingService.GetErrorActionResult(exception, "Authentication") as RedirectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Url.Contains("AuthenticationFailure"));
        }

        [TestMethod]
        public void ErrorHandlingService_CreateExamAccessError_ReturnsExamErrorView()
        {
            // Act
            var result = _errorHandlingService.CreateExamAccessError(ExamAccessErrorType.ExamNotFound) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ExamError", result.ViewName);
            Assert.IsInstanceOfType(result.Model, typeof(ExamAccessError));
        }

        [TestMethod]
        public void ExamAccessError_Create_SetsUserFriendlyMessage()
        {
            // Act
            var error = ExamAccessError.Create(ExamAccessErrorType.ExamNotPublished);

            // Assert
            Assert.IsNotNull(error.UserFriendlyMessage);
            Assert.IsTrue(error.UserFriendlyMessage.Contains("not yet available"));
        }

        [TestMethod]
        public void ExamAccessError_Create_NetworkConnectivityIssue_ShowsRetryOption()
        {
            // Act
            var error = ExamAccessError.Create(ExamAccessErrorType.NetworkConnectivityIssue);

            // Assert
            Assert.IsTrue(error.ShowRetryOption);
            Assert.IsTrue(error.ShowContactSupport);
        }

        [TestMethod]
        public void ExamAccessError_Create_TimerExpired_DoesNotShowRetryOption()
        {
            // Act
            var error = ExamAccessError.Create(ExamAccessErrorType.TimerExpired);

            // Assert
            Assert.IsFalse(error.ShowRetryOption);
            Assert.IsTrue(error.ShowContactSupport);
        }
    }
}