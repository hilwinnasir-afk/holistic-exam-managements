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
    /// Property-based tests for grading calculation system
    /// **Feature: holistic-examination-management-system, Property 8: Grading Accuracy and Consistency**
    /// **Feature: holistic-examination-management-system, Property 9: Score Calculation Properties**
    /// </summary>
    [TestClass]
    public class GradingCalculationPropertyTests
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

        /// <summary>
        /// **Validates: Requirements 7.1, 7.2**
        /// Property: Percentage calculation should always be between 0 and 100
        /// </summary>
        [TestMethod]
        public void Property_CalculatePercentage_AlwaysBetweenZeroAndHundred()
        {
            var testCases = new[]
            {
                new { Score = 0, Total = 10, Expected = 0.0m },
                new { Score = 5, Total = 10, Expected = 50.0m },
                new { Score = 10, Total = 10, Expected = 100.0m },
                new { Score = 1, Total = 3, Expected = 33.33m },
                new { Score = 2, Total = 3, Expected = 66.67m },
                new { Score = 7, Total = 9, Expected = 77.78m },
                new { Score = 15, Total = 20, Expected = 75.0m },
                new { Score = 0, Total = 1, Expected = 0.0m },
                new { Score = 1, Total = 1, Expected = 100.0m }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var percentage = _gradingService.CalculatePercentage(testCase.Score, testCase.Total);

                // Assert - Properties
                Assert.IsTrue(percentage >= 0.0m, 
                    $"Percentage should be >= 0 for score {testCase.Score}/{testCase.Total}, got {percentage}");
                
                Assert.IsTrue(percentage <= 100.0m, 
                    $"Percentage should be <= 100 for score {testCase.Score}/{testCase.Total}, got {percentage}");
                
                // Should match expected value (within reasonable precision)
                var difference = Math.Abs(percentage - testCase.Expected);
                Assert.IsTrue(difference < 0.01m, 
                    $"Percentage should be approximately {testCase.Expected} for score {testCase.Score}/{testCase.Total}, got {percentage}");
            }
        }

        /// <summary>
        /// **Validates: Requirements 7.1**
        /// Property: Score calculation should be monotonic (more correct answers = higher score)
        /// </summary>
        [TestMethod]
        public void Property_ScoreCalculation_Monotonic()
        {
            var examId = 1;
            var studentExamId = 1;

            // Create questions and choices
            var questions = new List<Question>();
            var choices = new List<Choice>();
            
            for (int i = 1; i <= 5; i++)
            {
                var question = new Question { QuestionId = i, ExamId = examId };
                questions.Add(question);
                
                // Add correct and incorrect choices for each question
                choices.Add(new Choice { ChoiceId = i * 2 - 1, QuestionId = i, IsCorrect = true });
                choices.Add(new Choice { ChoiceId = i * 2, QuestionId = i, IsCorrect = false });
            }

            var questionsQueryable = questions.AsQueryable();
            var choicesQueryable = choices.AsQueryable();

            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questionsQueryable.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questionsQueryable.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questionsQueryable.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questionsQueryable.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choicesQueryable.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choicesQueryable.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choicesQueryable.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choicesQueryable.GetEnumerator());

            // Test different numbers of correct answers
            var previousScore = -1;
            
            for (int correctAnswers = 0; correctAnswers <= 5; correctAnswers++)
            {
                // Create student answers with specified number of correct answers
                var studentAnswers = new List<StudentAnswer>();
                
                for (int i = 1; i <= 5; i++)
                {
                    var choiceId = i <= correctAnswers ? (i * 2 - 1) : (i * 2); // Correct or incorrect choice
                    studentAnswers.Add(new StudentAnswer 
                    { 
                        StudentExamId = studentExamId, 
                        QuestionId = i, 
                        ChoiceId = choiceId 
                    });
                }

                var answersQueryable = studentAnswers.AsQueryable();
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(answersQueryable.Provider);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(answersQueryable.Expression);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(answersQueryable.ElementType);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(answersQueryable.GetEnumerator());

                // Act
                var score = _gradingService.CalculateScore(studentExamId);

                // Assert - Property: Score should be monotonic
                Assert.AreEqual(correctAnswers, score, 
                    $"Score should equal number of correct answers: {correctAnswers}");
                
                Assert.IsTrue(score >= previousScore, 
                    $"Score should be monotonic: {score} should be >= {previousScore}");
                
                previousScore = score;
            }
        }

        /// <summary>
        /// **Validates: Requirements 7.2**
        /// Property: Percentage calculation should be mathematically consistent
        /// </summary>
        [TestMethod]
        public void Property_PercentageCalculation_MathematicallyConsistent()
        {
            var testCases = new[]
            {
                new { Score = 0, Total = 0 }, // Edge case: division by zero
                new { Score = 5, Total = 0 }, // Edge case: division by zero with score
                new { Score = 0, Total = 10 },
                new { Score = 10, Total = 10 },
                new { Score = 3, Total = 7 },
                new { Score = 17, Total = 23 },
                new { Score = 99, Total = 100 },
                new { Score = 1, Total = 100 }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var percentage = _gradingService.CalculatePercentage(testCase.Score, testCase.Total);

                // Assert - Properties
                if (testCase.Total == 0)
                {
                    // Property: Division by zero should return 0
                    Assert.AreEqual(0.0m, percentage, 
                        $"Division by zero should return 0, got {percentage}");
                }
                else
                {
                    // Property: Mathematical consistency
                    var expectedPercentage = (decimal)testCase.Score / testCase.Total * 100;
                    var difference = Math.Abs(percentage - expectedPercentage);
                    
                    Assert.IsTrue(difference < 0.01m, 
                        $"Percentage should be mathematically consistent: expected {expectedPercentage}, got {percentage}");
                    
                    // Property: Percentage should be proportional to score
                    if (testCase.Score == 0)
                    {
                        Assert.AreEqual(0.0m, percentage, "Zero score should give zero percentage");
                    }
                    else if (testCase.Score == testCase.Total)
                    {
                        Assert.AreEqual(100.0m, percentage, "Perfect score should give 100%");
                    }
                }
            }
        }

        /// <summary>
        /// **Validates: Requirements 7.3**
        /// Property: Grading should handle edge cases gracefully
        /// </summary>
        [TestMethod]
        public void Property_Grading_HandlesEdgeCasesGracefully()
        {
            var edgeCases = new[]
            {
                new { 
                    Description = "No questions", 
                    Questions = 0, 
                    CorrectAnswers = 0,
                    ExpectedScore = 0,
                    ExpectedPercentage = 0.0m
                },
                new { 
                    Description = "Single question correct", 
                    Questions = 1, 
                    CorrectAnswers = 1,
                    ExpectedScore = 1,
                    ExpectedPercentage = 100.0m
                },
                new { 
                    Description = "Single question incorrect", 
                    Questions = 1, 
                    CorrectAnswers = 0,
                    ExpectedScore = 0,
                    ExpectedPercentage = 0.0m
                },
                new { 
                    Description = "Large number of questions", 
                    Questions = 100, 
                    CorrectAnswers = 73,
                    ExpectedScore = 73,
                    ExpectedPercentage = 73.0m
                }
            };

            foreach (var edgeCase in edgeCases)
            {
                // Arrange
                var examId = 1;
                var studentExamId = 1;
                var questions = new List<Question>();
                var choices = new List<Choice>();
                var studentAnswers = new List<StudentAnswer>();

                // Create questions and choices
                for (int i = 1; i <= edgeCase.Questions; i++)
                {
                    var question = new Question { QuestionId = i, ExamId = examId };
                    questions.Add(question);
                    
                    if (edgeCase.Questions > 0) // Only add choices if there are questions
                    {
                        choices.Add(new Choice { ChoiceId = i * 2 - 1, QuestionId = i, IsCorrect = true });
                        choices.Add(new Choice { ChoiceId = i * 2, QuestionId = i, IsCorrect = false });
                        
                        // Create student answer (correct for first N questions)
                        var choiceId = i <= edgeCase.CorrectAnswers ? (i * 2 - 1) : (i * 2);
                        studentAnswers.Add(new StudentAnswer 
                        { 
                            StudentExamId = studentExamId, 
                            QuestionId = i, 
                            ChoiceId = choiceId 
                        });
                    }
                }

                // Setup mocks
                var questionsQueryable = questions.AsQueryable();
                var choicesQueryable = choices.AsQueryable();
                var answersQueryable = studentAnswers.AsQueryable();

                _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questionsQueryable.Provider);
                _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questionsQueryable.Expression);
                _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questionsQueryable.ElementType);
                _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questionsQueryable.GetEnumerator());

                _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choicesQueryable.Provider);
                _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choicesQueryable.Expression);
                _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choicesQueryable.ElementType);
                _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choicesQueryable.GetEnumerator());

                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(answersQueryable.Provider);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(answersQueryable.Expression);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(answersQueryable.ElementType);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(answersQueryable.GetEnumerator());

                // Act
                var score = _gradingService.CalculateScore(studentExamId);
                var percentage = _gradingService.CalculatePercentage(score, edgeCase.Questions);

                // Assert - Properties
                Assert.AreEqual(edgeCase.ExpectedScore, score, 
                    $"Score should be correct for edge case: {edgeCase.Description}");
                
                Assert.AreEqual(edgeCase.ExpectedPercentage, percentage, 
                    $"Percentage should be correct for edge case: {edgeCase.Description}");
            }
        }

        /// <summary>
        /// **Validates: Requirements 7.4**
        /// Property: Unanswered questions should be treated consistently
        /// </summary>
        [TestMethod]
        public void Property_UnansweredQuestions_TreatedConsistently()
        {
            var examId = 1;
            var studentExamId = 1;

            // Create 5 questions
            var questions = new List<Question>();
            var choices = new List<Choice>();
            
            for (int i = 1; i <= 5; i++)
            {
                var question = new Question { QuestionId = i, ExamId = examId };
                questions.Add(question);
                
                choices.Add(new Choice { ChoiceId = i * 2 - 1, QuestionId = i, IsCorrect = true });
                choices.Add(new Choice { ChoiceId = i * 2, QuestionId = i, IsCorrect = false });
            }

            var questionsQueryable = questions.AsQueryable();
            var choicesQueryable = choices.AsQueryable();

            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questionsQueryable.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questionsQueryable.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questionsQueryable.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questionsQueryable.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choicesQueryable.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choicesQueryable.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choicesQueryable.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choicesQueryable.GetEnumerator());

            var unansweredTestCases = new[]
            {
                new { Description = "All unanswered", AnsweredQuestions = 0 },
                new { Description = "Half unanswered", AnsweredQuestions = 2 },
                new { Description = "One unanswered", AnsweredQuestions = 4 },
                new { Description = "All answered", AnsweredQuestions = 5 }
            };

            foreach (var testCase in unansweredTestCases)
            {
                // Create student answers with some unanswered (null ChoiceId)
                var studentAnswers = new List<StudentAnswer>();
                
                for (int i = 1; i <= 5; i++)
                {
                    var choiceId = i <= testCase.AnsweredQuestions ? (int?)(i * 2 - 1) : null; // Correct answer or null
                    studentAnswers.Add(new StudentAnswer 
                    { 
                        StudentExamId = studentExamId, 
                        QuestionId = i, 
                        ChoiceId = choiceId 
                    });
                }

                var answersQueryable = studentAnswers.AsQueryable();
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(answersQueryable.Provider);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(answersQueryable.Expression);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(answersQueryable.ElementType);
                _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(answersQueryable.GetEnumerator());

                // Act
                var score = _gradingService.CalculateScore(studentExamId);

                // Assert - Property: Unanswered questions should count as incorrect (score 0)
                Assert.AreEqual(testCase.AnsweredQuestions, score, 
                    $"Score should equal answered questions for: {testCase.Description}");
                
                // Property: Unanswered questions should not contribute to score
                var unansweredQuestions = 5 - testCase.AnsweredQuestions;
                var maxPossibleScore = testCase.AnsweredQuestions; // All answered questions are correct in this test
                
                Assert.IsTrue(score <= maxPossibleScore, 
                    $"Score should not exceed answered questions for: {testCase.Description}");
            }
        }

        /// <summary>
        /// **Validates: Requirements 7.5**
        /// Property: Grading should be idempotent (same result when run multiple times)
        /// </summary>
        [TestMethod]
        public void Property_Grading_Idempotent()
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

            var questions = new List<Question>
            {
                new Question { QuestionId = 1, ExamId = examId },
                new Question { QuestionId = 2, ExamId = examId },
                new Question { QuestionId = 3, ExamId = examId }
            };

            var choices = new List<Choice>
            {
                new Choice { ChoiceId = 1, QuestionId = 1, IsCorrect = true },
                new Choice { ChoiceId = 2, QuestionId = 1, IsCorrect = false },
                new Choice { ChoiceId = 3, QuestionId = 2, IsCorrect = true },
                new Choice { ChoiceId = 4, QuestionId = 2, IsCorrect = false },
                new Choice { ChoiceId = 5, QuestionId = 3, IsCorrect = true },
                new Choice { ChoiceId = 6, QuestionId = 3, IsCorrect = false }
            };

            var studentAnswers = new List<StudentAnswer>
            {
                new StudentAnswer { StudentExamId = studentExamId, QuestionId = 1, ChoiceId = 1 }, // Correct
                new StudentAnswer { StudentExamId = studentExamId, QuestionId = 2, ChoiceId = 4 }, // Incorrect
                new StudentAnswer { StudentExamId = studentExamId, QuestionId = 3, ChoiceId = 5 }  // Correct
            };

            // Setup mocks
            _mockContext.Setup(c => c.StudentExams.Find(studentExamId)).Returns(studentExam);
            
            var questionsQueryable = questions.AsQueryable();
            var choicesQueryable = choices.AsQueryable();
            var answersQueryable = studentAnswers.AsQueryable();

            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Provider).Returns(questionsQueryable.Provider);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.Expression).Returns(questionsQueryable.Expression);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.ElementType).Returns(questionsQueryable.ElementType);
            _mockQuestionSet.As<IQueryable<Question>>().Setup(m => m.GetEnumerator()).Returns(questionsQueryable.GetEnumerator());

            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Provider).Returns(choicesQueryable.Provider);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.Expression).Returns(choicesQueryable.Expression);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.ElementType).Returns(choicesQueryable.ElementType);
            _mockChoiceSet.As<IQueryable<Choice>>().Setup(m => m.GetEnumerator()).Returns(choicesQueryable.GetEnumerator());

            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Provider).Returns(answersQueryable.Provider);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.Expression).Returns(answersQueryable.Expression);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.ElementType).Returns(answersQueryable.ElementType);
            _mockStudentAnswerSet.As<IQueryable<StudentAnswer>>().Setup(m => m.GetEnumerator()).Returns(answersQueryable.GetEnumerator());

            // Act - Grade multiple times
            var result1 = _gradingService.GradeExam(studentExamId);
            var score1 = studentExam.Score;
            var percentage1 = studentExam.Percentage;

            var result2 = _gradingService.GradeExam(studentExamId);
            var score2 = studentExam.Score;
            var percentage2 = studentExam.Percentage;

            var result3 = _gradingService.GradeExam(studentExamId);
            var score3 = studentExam.Score;
            var percentage3 = studentExam.Percentage;

            // Assert - Property: Idempotent (same results each time)
            Assert.IsTrue(result1 && result2 && result3, "All grading attempts should succeed");
            
            Assert.AreEqual(score1, score2, "Score should be identical on second grading");
            Assert.AreEqual(score2, score3, "Score should be identical on third grading");
            
            Assert.AreEqual(percentage1, percentage2, "Percentage should be identical on second grading");
            Assert.AreEqual(percentage2, percentage3, "Percentage should be identical on third grading");
            
            // Expected values: 2 correct out of 3 = 66.67%
            Assert.AreEqual(2, score1, "Score should be 2 (2 correct answers)");
            Assert.AreEqual(66.67m, Math.Round(percentage1.Value, 2), "Percentage should be 66.67%");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _gradingService?.Dispose();
        }
    }
}