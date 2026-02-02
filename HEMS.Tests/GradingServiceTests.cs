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
    [TestClass]
    public class GradingServiceTests
    {
        private GradingService _gradingService;
        private Mock<HEMSContext> _mockContext;
        private Mock<DbSet<StudentExam>> _mockStudentExamSet;
        private Mock<DbSet<StudentAnswer>> _mockStudentAnswerSet;
        private Mock<DbSet<Question>> _mockQuestionSet;
        private Mock<DbSet<Choice>> _mockChoiceSet;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _mockStudentExamSet = new Mock<DbSet<StudentExam>>();
            _mockStudentAnswerSet = new Mock<DbSet<StudentAnswer>>();
            _mockQuestionSet = new Mock<DbSet<Question>>();
            _mockChoiceSet = new Mock<DbSet<Choice>>();
            
            _mockContext.Setup(c => c.StudentExams).Returns(_mockStudentExamSet.Object);
            _mockContext.Setup(c => c.StudentAnswers).Returns(_mockStudentAnswerSet.Object);
            _mockContext.Setup(c => c.Questions).Returns(_mockQuestionSet.Object);
            _mockContext.Setup(c => c.Choices).Returns(_mockChoiceSet.Object);
            
            _gradingService = new GradingService(_mockContext.Object);
        }

        [TestMethod]
        public void GradeExam_AllCorrectAnswers_ReturnsFullScore()
        {
            // Arrange
            var studentExamId = 1;
            var examId = 1;
            
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                ExamId = examId,
                IsSubmitted = true,
                Score = null,
                Percentage = null
            };

            // Create questions and choices
            var question1 = new Question { QuestionId = 1, ExamId = examId };
            var question2 = new Question { QuestionId = 2, ExamId = examId };
            var question3 = new Question { QuestionId = 3, ExamId = examId };

            var choice1Correct = new Choice { ChoiceId = 1, QuestionId = 1, IsCorrect = true };
            var choice1Wrong = new Choice { ChoiceId = 2, QuestionId = 1, IsCorrect = false };
            var choice2Correct = new Choice { ChoiceId = 3, QuestionId = 2, IsCorrect = true };
            var choice2Wrong = new Choice { ChoiceId = 4, QuestionId = 2, IsCorrect = false };
            var choice3Correct = new Choice { ChoiceId = 5, QuestionId = 3, IsCorrect = true };
            var choice3Wrong = new Choice { ChoiceId = 6, QuestionId = 3, IsCorrect = false };

            // Student answers - all correct
            var answer1 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 1, ChoiceId = 1 };
            var answer2 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 2, ChoiceId = 3 };
            var answer3 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 3, ChoiceId = 5 };

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            var choices = new List<Choice> { choice1Correct, choice1Wrong, choice2Correct, choice2Wrong, choice3Correct, choice3Wrong }.AsQueryable();
            var studentAnswers = new List<StudentAnswer> { answer1, answer2, answer3 }.AsQueryable();

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);
            
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choices.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choices.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choices.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choices.GetEnumerator());

            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3, studentExam.Score); // 3 correct answers
            Assert.AreEqual(100.0m, studentExam.Percentage); // 100%
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void GradeExam_PartiallyCorrectAnswers_ReturnsPartialScore()
        {
            // Arrange
            var studentExamId = 1;
            var examId = 1;
            
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                ExamId = examId,
                IsSubmitted = true,
                Score = null,
                Percentage = null
            };

            // Create questions and choices
            var question1 = new Question { QuestionId = 1, ExamId = examId };
            var question2 = new Question { QuestionId = 2, ExamId = examId };
            var question3 = new Question { QuestionId = 3, ExamId = examId };

            var choice1Correct = new Choice { ChoiceId = 1, QuestionId = 1, IsCorrect = true };
            var choice1Wrong = new Choice { ChoiceId = 2, QuestionId = 1, IsCorrect = false };
            var choice2Correct = new Choice { ChoiceId = 3, QuestionId = 2, IsCorrect = true };
            var choice2Wrong = new Choice { ChoiceId = 4, QuestionId = 2, IsCorrect = false };
            var choice3Correct = new Choice { ChoiceId = 5, QuestionId = 3, IsCorrect = true };
            var choice3Wrong = new Choice { ChoiceId = 6, QuestionId = 3, IsCorrect = false };

            // Student answers - 2 correct, 1 wrong
            var answer1 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 1, ChoiceId = 1 }; // Correct
            var answer2 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 2, ChoiceId = 4 }; // Wrong
            var answer3 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 3, ChoiceId = 5 }; // Correct

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            var choices = new List<Choice> { choice1Correct, choice1Wrong, choice2Correct, choice2Wrong, choice3Correct, choice3Wrong }.AsQueryable();
            var studentAnswers = new List<StudentAnswer> { answer1, answer2, answer3 }.AsQueryable();

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);
            
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choices.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choices.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choices.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choices.GetEnumerator());

            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, studentExam.Score); // 2 correct answers
            Assert.AreEqual(66.67m, Math.Round(studentExam.Percentage.Value, 2)); // 66.67%
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void GradeExam_NoCorrectAnswers_ReturnsZeroScore()
        {
            // Arrange
            var studentExamId = 1;
            var examId = 1;
            
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                ExamId = examId,
                IsSubmitted = true,
                Score = null,
                Percentage = null
            };

            // Create questions and choices
            var question1 = new Question { QuestionId = 1, ExamId = examId };
            var question2 = new Question { QuestionId = 2, ExamId = examId };

            var choice1Correct = new Choice { ChoiceId = 1, QuestionId = 1, IsCorrect = true };
            var choice1Wrong = new Choice { ChoiceId = 2, QuestionId = 1, IsCorrect = false };
            var choice2Correct = new Choice { ChoiceId = 3, QuestionId = 2, IsCorrect = true };
            var choice2Wrong = new Choice { ChoiceId = 4, QuestionId = 2, IsCorrect = false };

            // Student answers - all wrong
            var answer1 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 1, ChoiceId = 2 }; // Wrong
            var answer2 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 2, ChoiceId = 4 }; // Wrong

            var questions = new List<Question> { question1, question2 }.AsQueryable();
            var choices = new List<Choice> { choice1Correct, choice1Wrong, choice2Correct, choice2Wrong }.AsQueryable();
            var studentAnswers = new List<StudentAnswer> { answer1, answer2 }.AsQueryable();

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);
            
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choices.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choices.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choices.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choices.GetEnumerator());

            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, studentExam.Score); // 0 correct answers
            Assert.AreEqual(0.0m, studentExam.Percentage); // 0%
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void GradeExam_UnansweredQuestions_CountsAsWrong()
        {
            // Arrange
            var studentExamId = 1;
            var examId = 1;
            
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                ExamId = examId,
                IsSubmitted = true,
                Score = null,
                Percentage = null
            };

            // Create questions and choices
            var question1 = new Question { QuestionId = 1, ExamId = examId };
            var question2 = new Question { QuestionId = 2, ExamId = examId };
            var question3 = new Question { QuestionId = 3, ExamId = examId };

            var choice1Correct = new Choice { ChoiceId = 1, QuestionId = 1, IsCorrect = true };
            var choice2Correct = new Choice { ChoiceId = 3, QuestionId = 2, IsCorrect = true };
            var choice3Correct = new Choice { ChoiceId = 5, QuestionId = 3, IsCorrect = true };

            // Student answers - 1 correct, 2 unanswered (null ChoiceId)
            var answer1 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 1, ChoiceId = 1 }; // Correct
            var answer2 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 2, ChoiceId = null }; // Unanswered
            var answer3 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 3, ChoiceId = null }; // Unanswered

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            var choices = new List<Choice> { choice1Correct, choice2Correct, choice3Correct }.AsQueryable();
            var studentAnswers = new List<StudentAnswer> { answer1, answer2, answer3 }.AsQueryable();

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);
            
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choices.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choices.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choices.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choices.GetEnumerator());

            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, studentExam.Score); // 1 correct answer
            Assert.AreEqual(33.33m, Math.Round(studentExam.Percentage.Value, 2)); // 33.33%
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void GradeExam_StudentExamNotFound_ReturnsFalse()
        {
            // Arrange
            var studentExamId = 999;
            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns((StudentExam)null);

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GradeExam_ExamNotSubmitted_ReturnsFalse()
        {
            // Arrange
            var studentExamId = 1;
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                IsSubmitted = false // Not submitted yet
            };

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GradeExam_AlreadyGraded_ReturnsTrue()
        {
            // Arrange
            var studentExamId = 1;
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                IsSubmitted = true,
                Score = 5, // Already graded
                Percentage = 83.33m
            };

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);

            // Act
            bool result = _gradingService.GradeExam(studentExamId);
            
            // Assert
            Assert.IsTrue(result); // Should still return true for already graded exams
        }

        [TestMethod]
        public void CalculateScore_ValidAnswers_ReturnsCorrectScore()
        {
            // Arrange
            var studentExamId = 1;
            
            // Create choices
            var choice1Correct = new Choice { ChoiceId = 1, QuestionId = 1, IsCorrect = true };
            var choice1Wrong = new Choice { ChoiceId = 2, QuestionId = 1, IsCorrect = false };
            var choice2Correct = new Choice { ChoiceId = 3, QuestionId = 2, IsCorrect = true };

            // Student answers - 1 correct, 1 wrong
            var answer1 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 1, ChoiceId = 1 }; // Correct
            var answer2 = new StudentAnswer { StudentExamId = studentExamId, QuestionId = 2, ChoiceId = 2 }; // Wrong (choice belongs to different question)

            var choices = new List<Choice> { choice1Correct, choice1Wrong, choice2Correct }.AsQueryable();
            var studentAnswers = new List<StudentAnswer> { answer1, answer2 }.AsQueryable();

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choices.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choices.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choices.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choices.GetEnumerator());

            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            int result = _gradingService.CalculateScore(studentExamId);
            
            // Assert
            Assert.AreEqual(1, result); // 1 correct answer
        }

        [TestMethod]
        public void CalculatePercentage_ValidScoreAndTotal_ReturnsCorrectPercentage()
        {
            // Arrange
            int score = 7;
            int totalQuestions = 10;

            // Act
            decimal result = _gradingService.CalculatePercentage(score, totalQuestions);
            
            // Assert
            Assert.AreEqual(70.0m, result);
        }

        [TestMethod]
        public void CalculatePercentage_ZeroTotal_ReturnsZero()
        {
            // Arrange
            int score = 5;
            int totalQuestions = 0;

            // Act
            decimal result = _gradingService.CalculatePercentage(score, totalQuestions);
            
            // Assert
            Assert.AreEqual(0.0m, result);
        }

        [TestMethod]
        public void CalculatePercentage_ZeroScore_ReturnsZero()
        {
            // Arrange
            int score = 0;
            int totalQuestions = 10;

            // Act
            decimal result = _gradingService.CalculatePercentage(score, totalQuestions);
            
            // Assert
            Assert.AreEqual(0.0m, result);
        }

        [TestMethod]
        public void CalculatePercentage_PerfectScore_ReturnsHundred()
        {
            // Arrange
            int score = 15;
            int totalQuestions = 15;

            // Act
            decimal result = _gradingService.CalculatePercentage(score, totalQuestions);
            
            // Assert
            Assert.AreEqual(100.0m, result);
        }

        [TestMethod]
        public void GetExamResults_ValidExam_ReturnsResults()
        {
            // Arrange
            var studentExamId = 1;
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                Score = 8,
                Percentage = 80.0m,
                IsSubmitted = true
            };

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);

            // Act
            var result = _gradingService.GetExamResults(studentExamId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(8, result.Score);
            Assert.AreEqual(80.0m, result.Percentage);
        }

        [TestMethod]
        public void GetExamResults_ExamNotFound_ReturnsNull()
        {
            // Arrange
            var studentExamId = 999;
            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns((StudentExam)null);

            // Act
            var result = _gradingService.GetExamResults(studentExamId);
            
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetExamResults_ExamNotSubmitted_ReturnsNull()
        {
            // Arrange
            var studentExamId = 1;
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                IsSubmitted = false // Not submitted
            };

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);

            // Act
            var result = _gradingService.GetExamResults(studentExamId);
            
            // Assert
            Assert.IsNull(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _gradingService?.Dispose();
        }
    }
}