using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using HEMS.Controllers;
using HEMS.Models;
using HEMS.Services;
using HEMS.Attributes;

namespace HEMS.Tests
{
    [TestClass]
    public class ExamTakingInterfaceTests
    {
        private Mock<HEMSContext> _mockContext;
        private Mock<IExamService> _mockExamService;
        private ExamController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _mockExamService = new Mock<IExamService>();
            var mockTimerService = new Mock<ITimerService>();
            _controller = new ExamController(_mockContext.Object, _mockExamService.Object, mockTimerService.Object);
        }

        [TestMethod]
        public void Task6_1_StudentExamModel_HasStartDateTime()
        {
            // Arrange & Act
            var studentExam = new StudentExam
            {
                StudentId = 1,
                ExamId = 1,
                StartDateTime = DateTime.Now,
                IsSubmitted = false
            };

            // Assert
            Assert.IsNotNull(studentExam.StartDateTime);
            Assert.AreEqual(1, studentExam.StudentId);
            Assert.AreEqual(1, studentExam.ExamId);
            Assert.IsFalse(studentExam.IsSubmitted);
        }

        [TestMethod]
        public void Task6_2_StudentAnswerModel_HasIsFlaggedField()
        {
            // Arrange & Act
            var studentAnswer = new StudentAnswer
            {
                StudentExamId = 1,
                QuestionId = 1,
                IsFlagged = true,
                LastModified = DateTime.Now
            };

            // Assert
            Assert.IsTrue(studentAnswer.IsFlagged);
            Assert.AreEqual(1, studentAnswer.StudentExamId);
            Assert.AreEqual(1, studentAnswer.QuestionId);
            Assert.IsNotNull(studentAnswer.LastModified);
        }

        [TestMethod]
        public void Task6_3_ExamController_HasStartExamMethod()
        {
            // Arrange
            var examId = 1;

            // Act & Assert - Method should exist and be callable
            var methodInfo = typeof(ExamController).GetMethod("StartExam");
            Assert.IsNotNull(methodInfo, "StartExam method should exist");
            Assert.AreEqual(typeof(ActionResult), methodInfo.ReturnType);
        }

        [TestMethod]
        public void Task6_3_ExamController_HasGetQuestionMethod()
        {
            // Arrange & Act
            var methodInfo = typeof(ExamController).GetMethod("Question");

            // Assert
            Assert.IsNotNull(methodInfo, "Question method should exist");
            Assert.AreEqual(typeof(ActionResult), methodInfo.ReturnType);
        }

        [TestMethod]
        public void Task6_5_ExamController_HasSaveAnswerMethod()
        {
            // Arrange & Act
            var methodInfo = typeof(ExamController).GetMethod("SaveAnswer");

            // Assert
            Assert.IsNotNull(methodInfo, "SaveAnswer method should exist");
            Assert.AreEqual(typeof(JsonResult), methodInfo.ReturnType);
        }

        [TestMethod]
        public void Task6_7_ExamController_HasToggleFlagMethod()
        {
            // Arrange & Act
            var methodInfo = typeof(ExamController).GetMethod("ToggleFlag");

            // Assert
            Assert.IsNotNull(methodInfo, "ToggleFlag method should exist");
            Assert.AreEqual(typeof(JsonResult), methodInfo.ReturnType);
        }

        [TestMethod]
        public void Task6_4_ExamQuestionViewModel_HasRequiredProperties()
        {
            // Arrange & Act
            var viewModel = new ExamQuestionViewModel
            {
                Question = new Question { QuestionId = 1, QuestionText = "Test Question" },
                SelectedChoiceId = 1,
                IsFlagged = true,
                AllQuestions = new List<Question>(),
                StudentAnswers = new List<StudentAnswer>(),
                CurrentQuestionId = 1,
                StudentExamId = 1
            };

            // Assert
            Assert.IsNotNull(viewModel.Question);
            Assert.AreEqual(1, viewModel.SelectedChoiceId);
            Assert.IsTrue(viewModel.IsFlagged);
            Assert.IsNotNull(viewModel.AllQuestions);
            Assert.IsNotNull(viewModel.StudentAnswers);
            Assert.AreEqual(1, viewModel.CurrentQuestionId);
            Assert.AreEqual(1, viewModel.StudentExamId);
        }

        [TestMethod]
        public void Task6_StudentExamModel_HasAllRequiredFields()
        {
            // Arrange & Act
            var studentExam = new StudentExam();

            // Assert - Check that all required properties exist
            var properties = typeof(StudentExam).GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToList();

            Assert.IsTrue(propertyNames.Contains("StudentExamId"));
            Assert.IsTrue(propertyNames.Contains("StudentId"));
            Assert.IsTrue(propertyNames.Contains("ExamId"));
            Assert.IsTrue(propertyNames.Contains("StartDateTime"));
            Assert.IsTrue(propertyNames.Contains("SubmitDateTime"));
            Assert.IsTrue(propertyNames.Contains("Score"));
            Assert.IsTrue(propertyNames.Contains("Percentage"));
            Assert.IsTrue(propertyNames.Contains("IsSubmitted"));
        }

        [TestMethod]
        public void Task6_StudentAnswerModel_HasAllRequiredFields()
        {
            // Arrange & Act
            var studentAnswer = new StudentAnswer();

            // Assert - Check that all required properties exist
            var properties = typeof(StudentAnswer).GetProperties();
            var propertyNames = properties.Select(p => p.Name).ToList();

            Assert.IsTrue(propertyNames.Contains("StudentAnswerId"));
            Assert.IsTrue(propertyNames.Contains("StudentExamId"));
            Assert.IsTrue(propertyNames.Contains("QuestionId"));
            Assert.IsTrue(propertyNames.Contains("ChoiceId"));
            Assert.IsTrue(propertyNames.Contains("IsFlagged"));
            Assert.IsTrue(propertyNames.Contains("LastModified"));
        }

        [TestMethod]
        public void Task6_ExamController_HasRoleAuthorizeAttribute()
        {
            // Arrange & Act
            var controllerType = typeof(ExamController);
            var attributes = controllerType.GetCustomAttributes(typeof(RoleAuthorizeAttribute), false);

            // Assert
            Assert.IsTrue(attributes.Length > 0, "ExamController should have RoleAuthorize attribute");
            var roleAttribute = attributes[0] as RoleAuthorizeAttribute;
            Assert.IsNotNull(roleAttribute);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _controller?.Dispose();
        }
    }
}