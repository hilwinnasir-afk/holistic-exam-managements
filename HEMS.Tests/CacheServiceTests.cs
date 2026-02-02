using HEMS.Models;
using HEMS.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEMS.Tests
{
    /// <summary>
    /// Unit tests for CacheService functionality
    /// Tests caching operations, expiration, and performance monitoring
    /// </summary>
    [TestClass]
    public class CacheServiceTests
    {
        private ICacheService _cacheService;

        [TestInitialize]
        public void Setup()
        {
            _cacheService = new CacheService();
            _cacheService.Clear(); // Start with clean cache
        }

        [TestCleanup]
        public void Cleanup()
        {
            _cacheService?.Clear();
            _cacheService?.Dispose();
        }

        #region Generic Cache Operations Tests

        [TestMethod]
        public void Set_And_Get_String_Value_Should_Work()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";

            // Act
            _cacheService.Set(key, value);
            var result = _cacheService.Get<string>(key);

            // Assert
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void Set_And_Get_Complex_Object_Should_Work()
        {
            // Arrange
            var key = "user_key";
            var user = new User
            {
                UserId = 1,
                Username = "test@university.edu",
                LoginPhaseCompleted = true,
                MustChangePassword = false
            };

            // Act
            _cacheService.Set(key, user);
            var result = _cacheService.Get<User>(key);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(user.UserId, result.UserId);
            Assert.AreEqual(user.Username, result.Username);
            Assert.AreEqual(user.LoginPhaseCompleted, result.LoginPhaseCompleted);
        }

        [TestMethod]
        public void Get_NonExistent_Key_Should_Return_Default()
        {
            // Act
            var result = _cacheService.Get<string>("non_existent_key");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Set_With_TimeSpan_Expiration_Should_Work()
        {
            // Arrange
            var key = "expiring_key";
            var value = "expiring_value";
            var expiration = TimeSpan.FromMilliseconds(100);

            // Act
            _cacheService.Set(key, value, expiration);
            var immediateResult = _cacheService.Get<string>(key);
            
            System.Threading.Thread.Sleep(150); // Wait for expiration
            var expiredResult = _cacheService.Get<string>(key);

            // Assert
            Assert.AreEqual(value, immediateResult);
            Assert.IsNull(expiredResult);
        }

        [TestMethod]
        public void Set_With_DateTime_Expiration_Should_Work()
        {
            // Arrange
            var key = "absolute_expiring_key";
            var value = "absolute_expiring_value";
            var expiration = DateTime.Now.AddMilliseconds(100);

            // Act
            _cacheService.Set(key, value, expiration);
            var immediateResult = _cacheService.Get<string>(key);
            
            System.Threading.Thread.Sleep(150); // Wait for expiration
            var expiredResult = _cacheService.Get<string>(key);

            // Assert
            Assert.AreEqual(value, immediateResult);
            Assert.IsNull(expiredResult);
        }

        [TestMethod]
        public void Remove_Should_Delete_Item()
        {
            // Arrange
            var key = "removable_key";
            var value = "removable_value";
            _cacheService.Set(key, value);

            // Act
            _cacheService.Remove(key);
            var result = _cacheService.Get<string>(key);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Exists_Should_Return_Correct_Status()
        {
            // Arrange
            var key = "existing_key";
            var value = "existing_value";

            // Act & Assert
            Assert.IsFalse(_cacheService.Exists(key));
            
            _cacheService.Set(key, value);
            Assert.IsTrue(_cacheService.Exists(key));
            
            _cacheService.Remove(key);
            Assert.IsFalse(_cacheService.Exists(key));
        }

        [TestMethod]
        public void RemoveByPattern_Should_Remove_Matching_Keys()
        {
            // Arrange
            _cacheService.Set("user:1", "user1");
            _cacheService.Set("user:2", "user2");
            _cacheService.Set("exam:1", "exam1");
            _cacheService.Set("config:setting", "value");

            // Act
            _cacheService.RemoveByPattern("user:*");

            // Assert
            Assert.IsNull(_cacheService.Get<string>("user:1"));
            Assert.IsNull(_cacheService.Get<string>("user:2"));
            Assert.IsNotNull(_cacheService.Get<string>("exam:1"));
            Assert.IsNotNull(_cacheService.Get<string>("config:setting"));
        }

        [TestMethod]
        public void Clear_Should_Remove_All_Items()
        {
            // Arrange
            _cacheService.Set("key1", "value1");
            _cacheService.Set("key2", "value2");
            _cacheService.Set("key3", "value3");

            // Act
            _cacheService.Clear();

            // Assert
            Assert.IsNull(_cacheService.Get<string>("key1"));
            Assert.IsNull(_cacheService.Get<string>("key2"));
            Assert.IsNull(_cacheService.Get<string>("key3"));
        }

        #endregion

        #region User Cache Tests

        [TestMethod]
        public void CacheUser_Should_Store_User_By_Multiple_Keys()
        {
            // Arrange
            var user = new User
            {
                UserId = 123,
                Username = "student@university.edu",
                Student = new Student { IdNumber = "STU001" }
            };

            // Act
            _cacheService.CacheUser(user);

            // Assert
            var userById = _cacheService.GetCachedUser(123);
            var userByEmail = _cacheService.GetCachedUserByEmail("student@university.edu");
            var userByIdNumber = _cacheService.GetCachedUserByIdNumber("STU001");

            Assert.IsNotNull(userById);
            Assert.IsNotNull(userByEmail);
            Assert.IsNotNull(userByIdNumber);
            Assert.AreEqual(user.UserId, userById.UserId);
            Assert.AreEqual(user.UserId, userByEmail.UserId);
            Assert.AreEqual(user.UserId, userByIdNumber.UserId);
        }

        [TestMethod]
        public void InvalidateUser_Should_Remove_All_User_Cache_Entries()
        {
            // Arrange
            var user = new User
            {
                UserId = 456,
                Username = "coordinator@university.edu",
                Student = new Student { IdNumber = "STU002" }
            };
            _cacheService.CacheUser(user);

            // Act
            _cacheService.InvalidateUser(456);

            // Assert
            Assert.IsNull(_cacheService.GetCachedUser(456));
            Assert.IsNull(_cacheService.GetCachedUserByEmail("coordinator@university.edu"));
            Assert.IsNull(_cacheService.GetCachedUserByIdNumber("STU002"));
        }

        [TestMethod]
        public void GetCachedUserByEmail_Should_Be_Case_Insensitive()
        {
            // Arrange
            var user = new User
            {
                UserId = 789,
                Username = "Test@University.EDU"
            };
            _cacheService.CacheUser(user);

            // Act
            var result1 = _cacheService.GetCachedUserByEmail("test@university.edu");
            var result2 = _cacheService.GetCachedUserByEmail("TEST@UNIVERSITY.EDU");

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.AreEqual(user.UserId, result1.UserId);
            Assert.AreEqual(user.UserId, result2.UserId);
        }

        #endregion

        #region Exam Session Cache Tests

        [TestMethod]
        public void CacheActiveExamSessions_Should_Store_And_Retrieve_Sessions()
        {
            // Arrange
            var sessions = new List<ExamSession>
            {
                new ExamSession { ExamSessionId = 1, ExamId = 1, IsActive = true },
                new ExamSession { ExamSessionId = 2, ExamId = 2, IsActive = true }
            };

            // Act
            _cacheService.CacheActiveExamSessions(sessions);
            var result = _cacheService.GetCachedActiveExamSessions();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].ExamSessionId);
            Assert.AreEqual(2, result[1].ExamSessionId);
        }

        [TestMethod]
        public void InvalidateExamSessions_Should_Remove_Sessions_Cache()
        {
            // Arrange
            var sessions = new List<ExamSession>
            {
                new ExamSession { ExamSessionId = 1, ExamId = 1, IsActive = true }
            };
            _cacheService.CacheActiveExamSessions(sessions);

            // Act
            _cacheService.InvalidateExamSessions();

            // Assert
            Assert.IsNull(_cacheService.GetCachedActiveExamSessions());
        }

        #endregion

        #region Exam Cache Tests

        [TestMethod]
        public void CacheExam_Should_Store_And_Retrieve_Exam()
        {
            // Arrange
            var exam = new Exam
            {
                ExamId = 100,
                Title = "Test Exam",
                Duration = 120,
                IsActive = true
            };

            // Act
            _cacheService.CacheExam(exam);
            var result = _cacheService.GetCachedExam(100);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(exam.ExamId, result.ExamId);
            Assert.AreEqual(exam.Title, result.Title);
            Assert.AreEqual(exam.Duration, result.Duration);
        }

        [TestMethod]
        public void CacheAvailableExams_Should_Store_Exams_By_Academic_Year()
        {
            // Arrange
            var exams = new List<Exam>
            {
                new Exam { ExamId = 1, Title = "Exam 1", AcademicYear = 2024 },
                new Exam { ExamId = 2, Title = "Exam 2", AcademicYear = 2024 }
            };

            // Act
            _cacheService.CacheAvailableExams(2024, exams);
            var result = _cacheService.GetCachedAvailableExams(2024);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Exam 1", result[0].Title);
            Assert.AreEqual("Exam 2", result[1].Title);
        }

        [TestMethod]
        public void InvalidateExam_Should_Remove_Exam_And_Questions()
        {
            // Arrange
            var exam = new Exam { ExamId = 200, Title = "Test Exam" };
            var questions = new List<Question>
            {
                new Question { QuestionId = 1, ExamId = 200, Text = "Question 1" }
            };
            
            _cacheService.CacheExam(exam);
            _cacheService.CacheExamQuestions(200, questions);

            // Act
            _cacheService.InvalidateExam(200);

            // Assert
            Assert.IsNull(_cacheService.GetCachedExam(200));
            Assert.IsNull(_cacheService.GetCachedExamQuestions(200));
        }

        #endregion

        #region Question Cache Tests

        [TestMethod]
        public void CacheExamQuestions_Should_Store_Questions_And_Choices()
        {
            // Arrange
            var questions = new List<Question>
            {
                new Question 
                { 
                    QuestionId = 1, 
                    ExamId = 1, 
                    Text = "What is 2+2?",
                    Choices = new List<Choice>
                    {
                        new Choice { ChoiceId = 1, QuestionId = 1, Text = "3", IsCorrect = false },
                        new Choice { ChoiceId = 2, QuestionId = 1, Text = "4", IsCorrect = true }
                    }
                }
            };

            // Act
            _cacheService.CacheExamQuestions(1, questions);

            // Assert
            var cachedQuestions = _cacheService.GetCachedExamQuestions(1);
            var cachedQuestion = _cacheService.GetCachedQuestion(1);
            var cachedChoices = _cacheService.GetCachedQuestionChoices(1);

            Assert.IsNotNull(cachedQuestions);
            Assert.AreEqual(1, cachedQuestions.Count);
            Assert.IsNotNull(cachedQuestion);
            Assert.AreEqual("What is 2+2?", cachedQuestion.Text);
            Assert.IsNotNull(cachedChoices);
            Assert.AreEqual(2, cachedChoices.Count);
        }

        [TestMethod]
        public void InvalidateQuestion_Should_Remove_Question_And_Choices()
        {
            // Arrange
            var question = new Question { QuestionId = 10, Text = "Test Question" };
            var choices = new List<Choice>
            {
                new Choice { ChoiceId = 1, QuestionId = 10, Text = "Choice 1" }
            };
            
            _cacheService.CacheQuestion(question);
            _cacheService.CacheQuestionChoices(10, choices);

            // Act
            _cacheService.InvalidateQuestion(10);

            // Assert
            Assert.IsNull(_cacheService.GetCachedQuestion(10));
            Assert.IsNull(_cacheService.GetCachedQuestionChoices(10));
        }

        #endregion

        #region Student Exam Cache Tests

        [TestMethod]
        public void CacheStudentExamStatus_Should_Store_And_Retrieve_Status()
        {
            // Arrange
            var studentExam = new StudentExam
            {
                StudentExamId = 1,
                StudentId = 100,
                ExamId = 200,
                StartTime = DateTime.Now,
                IsCompleted = false
            };

            // Act
            _cacheService.CacheStudentExamStatus(studentExam);
            var result = _cacheService.GetCachedStudentExamStatus(100, 200);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(studentExam.StudentExamId, result.StudentExamId);
            Assert.AreEqual(studentExam.StudentId, result.StudentId);
            Assert.AreEqual(studentExam.ExamId, result.ExamId);
        }

        [TestMethod]
        public void CacheStudentAnswers_Should_Store_Individual_And_Collection()
        {
            // Arrange
            var answers = new List<StudentAnswer>
            {
                new StudentAnswer { StudentExamId = 1, QuestionId = 1, SelectedChoiceId = 1 },
                new StudentAnswer { StudentExamId = 1, QuestionId = 2, SelectedChoiceId = 3 }
            };

            // Act
            _cacheService.CacheStudentAnswers(1, answers);

            // Assert
            var cachedAnswers = _cacheService.GetCachedStudentAnswers(1);
            var cachedAnswer1 = _cacheService.GetCachedStudentAnswer(1, 1);
            var cachedAnswer2 = _cacheService.GetCachedStudentAnswer(1, 2);

            Assert.IsNotNull(cachedAnswers);
            Assert.AreEqual(2, cachedAnswers.Count);
            Assert.IsNotNull(cachedAnswer1);
            Assert.AreEqual(1, cachedAnswer1.SelectedChoiceId);
            Assert.IsNotNull(cachedAnswer2);
            Assert.AreEqual(3, cachedAnswer2.SelectedChoiceId);
        }

        [TestMethod]
        public void InvalidateStudentAnswer_Should_Remove_Individual_And_Collection()
        {
            // Arrange
            var answers = new List<StudentAnswer>
            {
                new StudentAnswer { StudentExamId = 5, QuestionId = 10, SelectedChoiceId = 1 }
            };
            _cacheService.CacheStudentAnswers(5, answers);

            // Act
            _cacheService.InvalidateStudentAnswer(5, 10);

            // Assert
            Assert.IsNull(_cacheService.GetCachedStudentAnswer(5, 10));
            Assert.IsNull(_cacheService.GetCachedStudentAnswers(5));
        }

        #endregion

        #region Configuration Cache Tests

        [TestMethod]
        public void CacheConfiguration_Should_Store_And_Retrieve_Config()
        {
            // Arrange
            var key = "max_login_attempts";
            var value = "5";

            // Act
            _cacheService.CacheConfiguration(key, value);
            var result = _cacheService.GetCachedConfiguration(key);

            // Assert
            Assert.AreEqual(value, result);
        }

        [TestMethod]
        public void CacheUniversityDomains_Should_Store_Domain_List()
        {
            // Arrange
            var domains = new List<string> { "university.edu", "college.edu", "school.ac.uk" };

            // Act
            _cacheService.CacheUniversityDomains(domains);
            var result = _cacheService.GetCachedUniversityDomains();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("university.edu"));
            Assert.IsTrue(result.Contains("college.edu"));
            Assert.IsTrue(result.Contains("school.ac.uk"));
        }

        [TestMethod]
        public void InvalidateConfiguration_Should_Remove_Config_Item()
        {
            // Arrange
            _cacheService.CacheConfiguration("test_config", "test_value");

            // Act
            _cacheService.InvalidateConfiguration("test_config");

            // Assert
            Assert.IsNull(_cacheService.GetCachedConfiguration("test_config"));
        }

        #endregion

        #region Cache Statistics Tests

        [TestMethod]
        public void Cache_Statistics_Should_Track_Hits_And_Misses()
        {
            // Arrange
            _cacheService.ResetStatistics();
            _cacheService.Set("test_key", "test_value");

            // Act
            _cacheService.Get<string>("test_key"); // Hit
            _cacheService.Get<string>("non_existent"); // Miss
            _cacheService.Get<string>("test_key"); // Hit

            var hitRate = _cacheService.GetCacheHitRate();
            var stats = _cacheService.GetCacheStatistics();

            // Assert
            Assert.AreEqual(66.67, Math.Round(hitRate, 2)); // 2 hits out of 3 requests
            Assert.AreEqual(2L, stats["CacheHits"]);
            Assert.AreEqual(1L, stats["CacheMisses"]);
            Assert.AreEqual(3L, stats["TotalRequests"]);
        }

        [TestMethod]
        public void ResetStatistics_Should_Clear_Hit_Miss_Counters()
        {
            // Arrange
            _cacheService.Set("key", "value");
            _cacheService.Get<string>("key"); // Generate some stats
            _cacheService.Get<string>("missing");

            // Act
            _cacheService.ResetStatistics();
            var stats = _cacheService.GetCacheStatistics();

            // Assert
            Assert.AreEqual(0L, stats["CacheHits"]);
            Assert.AreEqual(0L, stats["CacheMisses"]);
            Assert.AreEqual(0L, stats["TotalRequests"]);
            Assert.AreEqual(0.0, stats["HitRate"]);
        }

        #endregion

        #region Edge Cases and Error Handling Tests

        [TestMethod]
        public void Set_With_Null_Key_Should_Not_Throw()
        {
            // Act & Assert - Should not throw exception
            _cacheService.Set<string>(null, "value");
            _cacheService.Set<string>("", "value");
            _cacheService.Set<string>("   ", "value");
        }

        [TestMethod]
        public void Set_With_Null_Value_Should_Not_Throw()
        {
            // Act & Assert - Should not throw exception
            _cacheService.Set<string>("key", null);
        }

        [TestMethod]
        public void Get_With_Null_Key_Should_Return_Default()
        {
            // Act
            var result1 = _cacheService.Get<string>(null);
            var result2 = _cacheService.Get<string>("");
            var result3 = _cacheService.Get<string>("   ");

            // Assert
            Assert.IsNull(result1);
            Assert.IsNull(result2);
            Assert.IsNull(result3);
        }

        [TestMethod]
        public void Remove_With_Null_Key_Should_Not_Throw()
        {
            // Act & Assert - Should not throw exception
            _cacheService.Remove(null);
            _cacheService.Remove("");
            _cacheService.Remove("   ");
        }

        [TestMethod]
        public void Exists_With_Null_Key_Should_Return_False()
        {
            // Act & Assert
            Assert.IsFalse(_cacheService.Exists(null));
            Assert.IsFalse(_cacheService.Exists(""));
            Assert.IsFalse(_cacheService.Exists("   "));
        }

        [TestMethod]
        public void CacheUser_With_Null_User_Should_Not_Throw()
        {
            // Act & Assert - Should not throw exception
            _cacheService.CacheUser(null);
        }

        [TestMethod]
        public void GetCachedUserByEmail_With_Null_Email_Should_Return_Null()
        {
            // Act & Assert
            Assert.IsNull(_cacheService.GetCachedUserByEmail(null));
            Assert.IsNull(_cacheService.GetCachedUserByEmail(""));
            Assert.IsNull(_cacheService.GetCachedUserByEmail("   "));
        }

        #endregion
    }
}
