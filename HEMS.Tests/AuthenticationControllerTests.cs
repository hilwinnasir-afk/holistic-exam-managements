using System;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using HEMS.Controllers;
using HEMS.Services;
using HEMS.Models;
using HEMS.Models.ViewModels;

namespace HEMS.Tests
{
    [TestClass]
    public class AuthenticationControllerTests
    {
        private Mock<IAuthenticationService> _mockAuthService;
        private AuthenticationController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockAuthService = new Mock<IAuthenticationService>();
            var mockSessionService = new Mock<ISessionService>();
            _controller = new AuthenticationController(_mockAuthService.Object, mockSessionService.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _controller?.Dispose();
        }

        [TestMethod]
        public void Phase1Login_GET_ReturnsViewWithModel()
        {
            // Act
            var result = _controller.Phase1Login() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Phase1LoginViewModel));
        }

        [TestMethod]
        public void Phase1Login_POST_ValidCredentials_RedirectsToPhase2Login()
        {
            // Arrange
            var model = new Phase1LoginViewModel
            {
                Email = "student@hems.edu",
                Password = "SE12317"
            };

            var user = new User { UserId = 1, Username = "student@hems.edu" };

            _mockAuthService.Setup(s => s.ValidatePhase1Login(model.Email, model.Password))
                           .Returns(true);
            _mockAuthService.Setup(s => s.GetUserByEmail(model.Email))
                           .Returns(user);
            _mockAuthService.Setup(s => s.CompletePhase1Login(user.UserId))
                           .Returns(true);

            // Act
            var result = _controller.Phase1Login(model) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Phase2Login", result.RouteValues["action"]);
        }

        [TestMethod]
        public void Phase1Login_POST_InvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var model = new Phase1LoginViewModel
            {
                Email = "student@hems.edu",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(s => s.ValidatePhase1Login(model.Email, model.Password))
                           .Returns(false);

            // Act
            var result = _controller.Phase1Login(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Phase1LoginViewModel));
            var returnedModel = result.Model as Phase1LoginViewModel;
            Assert.IsNotNull(returnedModel.ErrorMessage);
        }

        [TestMethod]
        public void Phase2Login_GET_ReturnsViewWithModel()
        {
            // Act
            var result = _controller.Phase2Login() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Phase2LoginViewModel));
        }

        [TestMethod]
        public void Phase2Login_POST_ValidCredentials_WithPasswordChangeRequired_RedirectsToChangePassword()
        {
            // Arrange
            var model = new Phase2LoginViewModel
            {
                IdNumber = "SE123",
                Password = "exam2024"
            };

            var user = new User { UserId = 1, Username = "student@hems.edu" };

            _mockAuthService.Setup(s => s.ValidatePhase2Login(model.IdNumber, model.Password))
                           .Returns(true);
            _mockAuthService.Setup(s => s.GetUserByIdNumber(model.IdNumber))
                           .Returns(user);
            _mockAuthService.Setup(s => s.MustChangePassword(user.UserId))
                           .Returns(true);

            // Act
            var result = _controller.Phase2Login(model) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ChangePassword", result.RouteValues["action"]);
        }

        [TestMethod]
        public void Phase2Login_POST_ValidCredentials_NoPasswordChangeRequired_RedirectsToHome()
        {
            // Arrange
            var model = new Phase2LoginViewModel
            {
                IdNumber = "SE123",
                Password = "exam2024"
            };

            var user = new User { UserId = 1, Username = "student@hems.edu" };

            _mockAuthService.Setup(s => s.ValidatePhase2Login(model.IdNumber, model.Password))
                           .Returns(true);
            _mockAuthService.Setup(s => s.GetUserByIdNumber(model.IdNumber))
                           .Returns(user);
            _mockAuthService.Setup(s => s.MustChangePassword(user.UserId))
                           .Returns(false);

            // Act
            var result = _controller.Phase2Login(model) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual("Home", result.RouteValues["controller"]);
        }

        [TestMethod]
        public void Phase2Login_POST_InvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var model = new Phase2LoginViewModel
            {
                IdNumber = "SE123",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(s => s.ValidatePhase2Login(model.IdNumber, model.Password))
                           .Returns(false);

            // Act
            var result = _controller.Phase2Login(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Phase2LoginViewModel));
            var returnedModel = result.Model as Phase2LoginViewModel;
            Assert.IsNotNull(returnedModel.ErrorMessage);
        }

        [TestMethod]
        public void ChangePassword_GET_ReturnsViewWithModel()
        {
            // Arrange
            _controller.Session["UserId"] = 1;

            // Act
            var result = _controller.ChangePassword() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(ChangePasswordViewModel));
        }

        [TestMethod]
        public void ChangePassword_POST_ValidPassword_RedirectsToHome()
        {
            // Arrange
            var model = new ChangePasswordViewModel
            {
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            _controller.Session["UserId"] = 1;

            _mockAuthService.Setup(s => s.ChangePassword(1, model.NewPassword))
                           .Returns(true);

            // Act
            var result = _controller.ChangePassword(model) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual("Home", result.RouteValues["controller"]);
        }

        [TestMethod]
        public void ChangePassword_POST_PasswordChangeFails_ReturnsViewWithError()
        {
            // Arrange
            var model = new ChangePasswordViewModel
            {
                NewPassword = "newpassword123",
                ConfirmPassword = "newpassword123"
            };

            _controller.Session["UserId"] = 1;

            _mockAuthService.Setup(s => s.ChangePassword(1, model.NewPassword))
                           .Returns(false);

            // Act
            var result = _controller.ChangePassword(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(ChangePasswordViewModel));
            var returnedModel = result.Model as ChangePasswordViewModel;
            Assert.IsNotNull(returnedModel.ErrorMessage);
        }

        [TestMethod]
        public void Logout_ClearsSessionAndRedirectsToPhase1Login()
        {
            // Act
            var result = _controller.Logout() as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Phase1Login", result.RouteValues["action"]);
        }

        [TestMethod]
        public void CheckAuthStatus_ReturnsJsonResult()
        {
            // Act
            var result = _controller.CheckAuthStatus() as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(JsonRequestBehavior.AllowGet, result.JsonRequestBehavior);
        }
    }
}