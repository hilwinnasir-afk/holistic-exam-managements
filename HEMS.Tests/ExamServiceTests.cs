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
    public class ExamServiceTests
    {
        private ExamService _examService;
        private Mock<HEMSContext> _mockContext;
        private Mock<DbSet<Exam>> _mockExamSet;
        private Mock<DbSet<Question>> _mockQuestionSet;
        private Mock<DbSet<Choice>> _mockChoiceSet;
        private Mock<DbSet<StudentExam>> _mockStudentExamSet;
        private Mock<DbSet<StudentAnswer>> _mockStudentAnswerSet;

        [TestInitialize]
        public void Setup()
        {
            _mockContext = new Mock<HEMSContext>();
            _mockExamSet = new Mock<DbSet<Exam>>();
            _mockQuestionSet = new Mock<DbSet<Question>>();
            _mockChoiceSet = new Mock<DbSet<Choice>>();
            _mockStudentExamSet = new Mock<DbSet<StudentExam>>();
            _mockStudentAnswerSet = new Mock<DbSet<StudentAnswer>>();
            
            _mockContext.Setup(c => c.Exams).Returns(_mockExamSet.Object);
            _mockContext.Setup(c => c.Questions).Returns(_mockQuestionSet.Object);
            _mockContext.Setup(c => c.Choices).Returns(_mockChoiceSet.Object);
            _mockContext.Setup(c => c.StudentExams).Returns(_mockStudentExamSet.Object);
            _mockContext.Setup(c => c.StudentAnswers).Returns(_mockStudentAnswerSet.Object);
            
            _examService = new ExamService(_mockContext.Object);
        }

        [TestMethod]
        public void CreateExam_ValidExam_ReturnsTrue()
        {
            // Arrange
            var exam = new Exam
            {
                Title = "Test Exam",
                AcademicYear = "2024",
                DurationMinutes = 60,
                IsPublished = false
            };

            var existingExams = new List<Exam>().AsQueryable();
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.Provider).Returns(existingExams.Provider);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.Expression).Returns(existingExams.Expression);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.ElementType).Returns(existingExams.ElementType);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.GetEnumerator()).Returns(existingExams.GetEnumerator());

            _mockContext.Setup(c => c.Exams.Add(It.IsAny<Exam>()));

            // Act
            bool result = _examService.CreateExam(exam);
            
            // Assert
            Assert.IsTrue(result);
            _mockContext.Verify(c => c.Exams.Add(It.IsAny<Exam>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void CreateExam_DuplicateAcademicYear_ReturnsFalse()
        {
            // Arrange
            var academicYear = "2024";
            var existingExam = new Exam
            {
                ExamId = 1,
                Title = "Existing Exam",
                AcademicYear = academicYear,
                DurationMinutes = 60,
                IsPublished = true
            };

            var newExam = new Exam
            {
                Title = "New Exam",
                AcademicYear = academicYear, // Same academic year
                DurationMinutes = 90,
                IsPublished = false
            };

            var existingExams = new List<Exam> { existingExam }.AsQueryable();
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.Provider).Returns(existingExams.Provider);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.Expression).Returns(existingExams.Expression);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.ElementType).Returns(existingExams.ElementType);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.GetEnumerator()).Returns(existingExams.GetEnumerator());

            // Act
            bool result = _examService.CreateExam(newExam);
            
            // Assert
            Assert.IsFalse(result);
            _mockContext.Verify(c => c.Exams.Add(It.IsAny<Exam>()), Times.Never);
            _mockContext.Verify(c => c.SaveChanges(), Times.Never);
        }

        [TestMethod]
        public void CreateExam_NullExam_ReturnsFalse()
        {
            // Arrange
            Exam exam = null;

            // Act
            bool result = _examService.CreateExam(exam);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetExamById_ValidId_ReturnsExam()
        {
            // Arrange
            var examId = 1;
            var exam = new Exam
            {
                ExamId = examId,
                Title = "Test Exam",
                AcademicYear = "2024",
                DurationMinutes = 60,
                IsPublished = true
            };

            _mockContext.Setup(c => c.Exams.Find(examId)).Returns(exam);

            // Act
            var result = _examService.GetExamById(examId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(examId, result.ExamId);
            Assert.AreEqual("Test Exam", result.Title);
        }

        [TestMethod]
        public void GetExamById_InvalidId_ReturnsNull()
        {
            // Arrange
            var examId = 999;
            _mockContext.Setup(c => c.Exams.Find(examId)).Returns((Exam)null);

            // Act
            var result = _examService.GetExamById(examId);
            
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetAvailableExams_PublishedExams_ReturnsPublishedOnly()
        {
            // Arrange
            var publishedExam = new Exam
            {
                ExamId = 1,
                Title = "Published Exam",
                AcademicYear = "2024",
                IsPublished = true
            };

            var unpublishedExam = new Exam
            {
                ExamId = 2,
                Title = "Unpublished Exam",
                AcademicYear = "2023",
                IsPublished = false
            };

            var exams = new List<Exam> { publishedExam, unpublishedExam }.AsQueryable();
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.Provider).Returns(exams.Provider);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.Expression).Returns(exams.Expression);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.ElementType).Returns(exams.ElementType);
            _mockExamSet.As<IQueryable<Exam>>().Setup(m => m.GetEnumerator()).Returns(exams.GetEnumerator());

            // Act
            var result = _examService.GetAvailableExams();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Published Exam", result.First().Title);
            Assert.IsTrue(result.First().IsPublished);
        }

        [TestMethod]
        public void PublishExam_ValidExam_ReturnsTrue()
        {
            // Arrange
            var examId = 1;
            var exam = new Exam
            {
                ExamId = examId,
                Title = "Test Exam",
                AcademicYear = "2024",
                DurationMinutes = 60,
                IsPublished = false
            };

            _mockContext.Setup(c => c.Exams.Find(examId)).Returns(exam);

            // Act
            bool result = _examService.PublishExam(examId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(exam.IsPublished);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void PublishExam_ExamNotFound_ReturnsFalse()
        {
            // Arrange
            var examId = 999;
            _mockContext.Setup(c => c.Exams.Find(examId)).Returns((Exam)null);

            // Act
            bool result = _examService.PublishExam(examId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void PublishExam_AlreadyPublished_ReturnsTrue()
        {
            // Arrange
            var examId = 1;
            var exam = new Exam
            {
                ExamId = examId,
                Title = "Test Exam",
                AcademicYear = "2024",
                DurationMinutes = 60,
                IsPublished = true // Already published
            };

            _mockContext.Setup(c => c.Exams.Find(examId)).Returns(exam);

            // Act
            bool result = _examService.PublishExam(examId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(exam.IsPublished);
        }

        [TestMethod]
        public void AddQuestion_ValidQuestion_ReturnsTrue()
        {
            // Arrange
            var examId = 1;
            var question = new Question
            {
                ExamId = examId,
                QuestionText = "What is 2+2?",
                QuestionOrder = 1
            };

            var exam = new Exam { ExamId = examId, IsPublished = false };
            _mockContext.Setup(c => c.Exams.Find(examId)).Returns(exam);
            _mockContext.Setup(c => c.Questions.Add(It.IsAny<Question>()));

            // Act
            bool result = _examService.AddQuestion(question);
            
            // Assert
            Assert.IsTrue(result);
            _mockContext.Verify(c => c.Questions.Add(It.IsAny<Question>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void AddQuestion_ExamNotFound_ReturnsFalse()
        {
            // Arrange
            var examId = 999;
            var question = new Question
            {
                ExamId = examId,
                QuestionText = "What is 2+2?",
                QuestionOrder = 1
            };

            _mockContext.Setup(c => c.Exams.Find(examId)).Returns((Exam)null);

            // Act
            bool result = _examService.AddQuestion(question);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddQuestion_ExamAlreadyPublished_ReturnsFalse()
        {
            // Arrange
            var examId = 1;
            var question = new Question
            {
                ExamId = examId,
                QuestionText = "What is 2+2?",
                QuestionOrder = 1
            };

            var exam = new Exam { ExamId = examId, IsPublished = true }; // Already published
            _mockContext.Setup(c => c.Exams.Find(examId)).Returns(exam);

            // Act
            bool result = _examService.AddQuestion(question);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddQuestion_NullQuestion_ReturnsFalse()
        {
            // Arrange
            Question question = null;

            // Act
            bool result = _examService.AddQuestion(question);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddChoice_ValidChoice_ReturnsTrue()
        {
            // Arrange
            var questionId = 1;
            var choice = new Choice
            {
                QuestionId = questionId,
                ChoiceText = "4",
                IsCorrect = true
            };

            var question = new Question { QuestionId = questionId };
            _mockContext.Setup(c => c.Questions.Find(questionId)).Returns(question);
            _mockContext.Setup(c => c.Choices.Add(It.IsAny<Choice>()));

            // Act
            bool result = _examService.AddChoice(choice);
            
            // Assert
            Assert.IsTrue(result);
            _mockContext.Verify(c => c.Choices.Add(It.IsAny<Choice>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void AddChoice_QuestionNotFound_ReturnsFalse()
        {
            // Arrange
            var questionId = 999;
            var choice = new Choice
            {
                QuestionId = questionId,
                ChoiceText = "4",
                IsCorrect = true
            };

            _mockContext.Setup(c => c.Questions.Find(questionId)).Returns((Question)null);

            // Act
            bool result = _examService.AddChoice(choice);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AddChoice_NullChoice_ReturnsFalse()
        {
            // Arrange
            Choice choice = null;

            // Act
            bool result = _examService.AddChoice(choice);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetExamQuestions_ValidExam_ReturnsOrderedQuestions()
        {
            // Arrange
            var examId = 1;
            var question1 = new Question { QuestionId = 1, ExamId = examId, QuestionOrder = 2, QuestionText = "Question 2" };
            var question2 = new Question { QuestionId = 2, ExamId = examId, QuestionOrder = 1, QuestionText = "Question 1" };
            var question3 = new Question { QuestionId = 3, ExamId = examId, QuestionOrder = 3, QuestionText = "Question 3" };

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            // Act
            var result = _examService.GetExamQuestions(examId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            
            var orderedQuestions = result.ToList();
            Assert.AreEqual("Question 1", orderedQuestions[0].QuestionText);
            Assert.AreEqual("Question 2", orderedQuestions[1].QuestionText);
            Assert.AreEqual("Question 3", orderedQuestions[2].QuestionText);
        }

        [TestMethod]
        public void GetExamQuestions_NoQuestions_ReturnsEmptyList()
        {
            // Arrange
            var examId = 1;
            var questions = new List<Question>().AsQueryable();
            
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            // Act
            var result = _examService.GetExamQuestions(examId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetNextQuestion_HasNextQuestion_ReturnsNextQuestion()
        {
            // Arrange
            var examId = 1;
            var currentQuestionId = 1;
            
            var question1 = new Question { QuestionId = 1, ExamId = examId, QuestionOrder = 1 };
            var question2 = new Question { QuestionId = 2, ExamId = examId, QuestionOrder = 2 };
            var question3 = new Question { QuestionId = 3, ExamId = examId, QuestionOrder = 3 };

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            // Act
            var result = _examService.GetNextQuestion(examId, currentQuestionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.QuestionId);
            Assert.AreEqual(2, result.QuestionOrder);
        }

        [TestMethod]
        public void GetNextQuestion_LastQuestion_ReturnsNull()
        {
            // Arrange
            var examId = 1;
            var currentQuestionId = 3; // Last question
            
            var question1 = new Question { QuestionId = 1, ExamId = examId, QuestionOrder = 1 };
            var question2 = new Question { QuestionId = 2, ExamId = examId, QuestionOrder = 2 };
            var question3 = new Question { QuestionId = 3, ExamId = examId, QuestionOrder = 3 };

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            // Act
            var result = _examService.GetNextQuestion(examId, currentQuestionId);
            
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetPreviousQuestion_HasPreviousQuestion_ReturnsPreviousQuestion()
        {
            // Arrange
            var examId = 1;
            var currentQuestionId = 2;
            
            var question1 = new Question { QuestionId = 1, ExamId = examId, QuestionOrder = 1 };
            var question2 = new Question { QuestionId = 2, ExamId = examId, QuestionOrder = 2 };
            var question3 = new Question { QuestionId = 3, ExamId = examId, QuestionOrder = 3 };

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            // Act
            var result = _examService.GetPreviousQuestion(examId, currentQuestionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.QuestionId);
            Assert.AreEqual(1, result.QuestionOrder);
        }

        [TestMethod]
        public void GetPreviousQuestion_FirstQuestion_ReturnsNull()
        {
            // Arrange
            var examId = 1;
            var currentQuestionId = 1; // First question
            
            var question1 = new Question { QuestionId = 1, ExamId = examId, QuestionOrder = 1 };
            var question2 = new Question { QuestionId = 2, ExamId = examId, QuestionOrder = 2 };
            var question3 = new Question { QuestionId = 3, ExamId = examId, QuestionOrder = 3 };

            var questions = new List<Question> { question1, question2, question3 }.AsQueryable();
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questions.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questions.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questions.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questions.GetEnumerator());

            // Act
            var result = _examService.GetPreviousQuestion(examId, currentQuestionId);
            
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SaveAnswer_ValidAnswer_ReturnsTrue()
        {
            // Arrange
            var studentExamId = 1;
            var questionId = 1;
            var choiceId = 2;

            var studentAnswer = new StudentAnswer
            {
                StudentExamId = studentExamId,
                QuestionId = questionId,
                ChoiceId = null
            };

            var studentAnswers = new List<StudentAnswer> { studentAnswer }.AsQueryable();
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            bool result = _examService.SaveAnswer(studentExamId, questionId, choiceId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(choiceId, studentAnswer.ChoiceId);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void SaveAnswer_AnswerNotFound_ReturnsFalse()
        {
            // Arrange
            var studentExamId = 1;
            var questionId = 999; // Non-existent question
            var choiceId = 2;

            var studentAnswers = new List<StudentAnswer>().AsQueryable(); // No answers
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(studentAnswers.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(studentAnswers.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(studentAnswers.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(studentAnswers.GetEnumerator());

            // Act
            bool result = _examService.SaveAnswer(studentExamId, questionId, choiceId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SubmitExam_ValidExam_ReturnsTrue()
        {
            // Arrange
            var studentExamId = 1;
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                IsSubmitted = false
            };

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);

            // Act
            bool result = _examService.SubmitExam(studentExamId);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(studentExam.IsSubmitted);
            Assert.IsNotNull(studentExam.SubmitDateTime);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [TestMethod]
        public void SubmitExam_ExamNotFound_ReturnsFalse()
        {
            // Arrange
            var studentExamId = 999;
            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns((StudentExam)null);

            // Act
            bool result = _examService.SubmitExam(studentExamId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SubmitExam_AlreadySubmitted_ReturnsFalse()
        {
            // Arrange
            var studentExamId = 1;
            var studentExam = new StudentExam
            {
                StudentExamId = studentExamId,
                IsSubmitted = true // Already submitted
            };

            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);

            // Act
            bool result = _examService.SubmitExam(studentExamId);
            
            // Assert
            Assert.IsFalse(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _examService?.Dispose();
        }
    }
}