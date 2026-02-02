using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Attributes;

namespace HEMS.Tests
{
    /// <summary>
    /// Tests for custom validation attributes
    /// </summary>
    [TestClass]
    public class ValidationAttributeTests
    {
        #region StudentIdAttribute Tests

        [TestMethod]
        public void StudentIdAttribute_ValidId_ReturnsTrue()
        {
            // Arrange
            var attribute = new StudentIdAttribute();
            var validIds = new[] { "SE123", "CS2024", "IT001", "ABC123XYZ" };

            // Act & Assert
            foreach (var id in validIds)
            {
                Assert.IsTrue(attribute.IsValid(id), $"ID '{id}' should be valid");
            }
        }

        [TestMethod]
        public void StudentIdAttribute_InvalidId_ReturnsFalse()
        {
            // Arrange
            var attribute = new StudentIdAttribute();
            var invalidIds = new[] { "SE@123", "CS-2024", "IT 001", "A", "", "ThisIsAVeryLongStudentIdNumberThatExceedsTheLimit" };

            // Act & Assert
            foreach (var id in invalidIds)
            {
                Assert.IsFalse(attribute.IsValid(id), $"ID '{id}' should be invalid");
            }
        }

        [TestMethod]
        public void StudentIdAttribute_NullValue_ReturnsTrue()
        {
            // Arrange
            var attribute = new StudentIdAttribute();

            // Act & Assert
            Assert.IsTrue(attribute.IsValid(null), "Null value should return true (let Required handle it)");
        }

        #endregion

        #region BatchYearAttribute Tests

        [TestMethod]
        public void BatchYearAttribute_ValidYear_ReturnsTrue()
        {
            // Arrange
            var attribute = new BatchYearAttribute();
            var validYears = new[] { 2000, 2024, 2050 };

            // Act & Assert
            foreach (var year in validYears)
            {
                Assert.IsTrue(attribute.IsValid(year), $"Year {year} should be valid");
            }
        }

        [TestMethod]
        public void BatchYearAttribute_InvalidYear_ReturnsFalse()
        {
            // Arrange
            var attribute = new BatchYearAttribute();
            var invalidYears = new[] { 1999, 2051, -1, 0 };

            // Act & Assert
            foreach (var year in invalidYears)
            {
                Assert.IsFalse(attribute.IsValid(year), $"Year {year} should be invalid");
            }
        }

        [TestMethod]
        public void BatchYearAttribute_CustomRange_WorksCorrectly()
        {
            // Arrange
            var attribute = new BatchYearAttribute(2020, 2030);

            // Act & Assert
            Assert.IsTrue(attribute.IsValid(2025), "Year within custom range should be valid");
            Assert.IsFalse(attribute.IsValid(2019), "Year below custom range should be invalid");
            Assert.IsFalse(attribute.IsValid(2031), "Year above custom range should be invalid");
        }

        #endregion

        #region AcademicYearAttribute Tests

        [TestMethod]
        public void AcademicYearAttribute_ValidYear_ReturnsTrue()
        {
            // Arrange
            var attribute = new AcademicYearAttribute();
            var validYears = new[] { 2000, 2024, 2050 };

            // Act & Assert
            foreach (var year in validYears)
            {
                Assert.IsTrue(attribute.IsValid(year), $"Academic year {year} should be valid");
            }
        }

        [TestMethod]
        public void AcademicYearAttribute_InvalidYear_ReturnsFalse()
        {
            // Arrange
            var attribute = new AcademicYearAttribute();
            var invalidYears = new[] { 1999, 2051, -1 };

            // Act & Assert
            foreach (var year in invalidYears)
            {
                Assert.IsFalse(attribute.IsValid(year), $"Academic year {year} should be invalid");
            }
        }

        #endregion

        #region ExamDurationAttribute Tests

        [TestMethod]
        public void ExamDurationAttribute_ValidDuration_ReturnsTrue()
        {
            // Arrange
            var attribute = new ExamDurationAttribute();
            var validDurations = new[] { 30, 60, 120, 240, 480 };

            // Act & Assert
            foreach (var duration in validDurations)
            {
                Assert.IsTrue(attribute.IsValid(duration), $"Duration {duration} minutes should be valid");
            }
        }

        [TestMethod]
        public void ExamDurationAttribute_InvalidDuration_ReturnsFalse()
        {
            // Arrange
            var attribute = new ExamDurationAttribute();
            var invalidDurations = new[] { 29, 481, 0, -1 };

            // Act & Assert
            foreach (var duration in invalidDurations)
            {
                Assert.IsFalse(attribute.IsValid(duration), $"Duration {duration} minutes should be invalid");
            }
        }

        [TestMethod]
        public void ExamDurationAttribute_CustomRange_WorksCorrectly()
        {
            // Arrange
            var attribute = new ExamDurationAttribute(60, 180); // 1-3 hours

            // Act & Assert
            Assert.IsTrue(attribute.IsValid(120), "Duration within custom range should be valid");
            Assert.IsFalse(attribute.IsValid(30), "Duration below custom range should be invalid");
            Assert.IsFalse(attribute.IsValid(240), "Duration above custom range should be invalid");
        }

        #endregion

        #region QuestionTextAttribute Tests

        [TestMethod]
        public void QuestionTextAttribute_ValidText_ReturnsTrue()
        {
            // Arrange
            var attribute = new QuestionTextAttribute();
            var validTexts = new[]
            {
                "What is software engineering?",
                "Explain the principles of object-oriented programming and provide examples.",
                "<p>What are the main <strong>benefits</strong> of using version control systems?</p>"
            };

            // Act & Assert
            foreach (var text in validTexts)
            {
                Assert.IsTrue(attribute.IsValid(text), $"Question text should be valid");
            }
        }

        [TestMethod]
        public void QuestionTextAttribute_InvalidText_ReturnsFalse()
        {
            // Arrange
            var attribute = new QuestionTextAttribute();
            var invalidTexts = new[]
            {
                "What?", // Too short
                "", // Empty
                new string('A', 2001) // Too long
            };

            // Act & Assert
            foreach (var text in invalidTexts)
            {
                Assert.IsFalse(attribute.IsValid(text), $"Question text should be invalid");
            }
        }

        [TestMethod]
        public void QuestionTextAttribute_HtmlContent_StripsTagsForValidation()
        {
            // Arrange
            var attribute = new QuestionTextAttribute(10, 50);
            var htmlText = "<p>Short</p>"; // Should be valid after stripping tags

            // Act & Assert
            Assert.IsTrue(attribute.IsValid(htmlText), "HTML content should be valid after stripping tags");
        }

        #endregion

        #region ChoiceTextAttribute Tests

        [TestMethod]
        public void ChoiceTextAttribute_ValidText_ReturnsTrue()
        {
            // Arrange
            var attribute = new ChoiceTextAttribute();
            var validTexts = new[]
            {
                "A",
                "Option A: This is a valid choice",
                "A detailed explanation of the choice with multiple sentences."
            };

            // Act & Assert
            foreach (var text in validTexts)
            {
                Assert.IsTrue(attribute.IsValid(text), $"Choice text should be valid");
            }
        }

        [TestMethod]
        public void ChoiceTextAttribute_InvalidText_ReturnsFalse()
        {
            // Arrange
            var attribute = new ChoiceTextAttribute();
            var invalidTexts = new[]
            {
                "", // Empty
                new string('A', 501) // Too long
            };

            // Act & Assert
            foreach (var text in invalidTexts)
            {
                Assert.IsFalse(attribute.IsValid(text), $"Choice text should be invalid");
            }
        }

        #endregion

        #region ExamTitleAttribute Tests

        [TestMethod]
        public void ExamTitleAttribute_ValidTitle_ReturnsTrue()
        {
            // Arrange
            var attribute = new ExamTitleAttribute();
            var validTitles = new[]
            {
                "Final Exam",
                "Software Engineering Midterm Exam 2024",
                "Computer Science Final Assessment (Spring 2024)"
            };

            // Act & Assert
            foreach (var title in validTitles)
            {
                Assert.IsTrue(attribute.IsValid(title), $"Exam title '{title}' should be valid");
            }
        }

        [TestMethod]
        public void ExamTitleAttribute_InvalidTitle_ReturnsFalse()
        {
            // Arrange
            var attribute = new ExamTitleAttribute();
            var invalidTitles = new[]
            {
                "SE", // Too short
                new string('A', 201), // Too long
                "Exam@#$%", // Invalid characters
                ""
            };

            // Act & Assert
            foreach (var title in invalidTitles)
            {
                Assert.IsFalse(attribute.IsValid(title), $"Exam title '{title}' should be invalid");
            }
        }

        #endregion

        #region SessionPasswordAttribute Tests

        [TestMethod]
        public void SessionPasswordAttribute_ValidPassword_ReturnsTrue()
        {
            // Arrange
            var attribute = new SessionPasswordAttribute();
            var validPasswords = new[]
            {
                "exam2024",
                "session123",
                "test@exam!",
                "Spring2024Exam"
            };

            // Act & Assert
            foreach (var password in validPasswords)
            {
                Assert.IsTrue(attribute.IsValid(password), $"Session password '{password}' should be valid");
            }
        }

        [TestMethod]
        public void SessionPasswordAttribute_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            var attribute = new SessionPasswordAttribute();
            var invalidPasswords = new[]
            {
                "abc", // Too short
                new string('A', 51), // Too long
                "test\npassword", // Invalid characters
                ""
            };

            // Act & Assert
            foreach (var password in invalidPasswords)
            {
                Assert.IsFalse(attribute.IsValid(password), $"Session password '{password}' should be invalid");
            }
        }

        [TestMethod]
        public void SessionPasswordAttribute_CustomRange_WorksCorrectly()
        {
            // Arrange
            var attribute = new SessionPasswordAttribute(8, 20);

            // Act & Assert
            Assert.IsTrue(attribute.IsValid("password123"), "Password within custom range should be valid");
            Assert.IsFalse(attribute.IsValid("short"), "Password below custom range should be invalid");
            Assert.IsFalse(attribute.IsValid("verylongpasswordthatexceedslimit"), "Password above custom range should be invalid");
        }

        #endregion

        #region Error Message Tests

        [TestMethod]
        public void ValidationAttributes_FormatErrorMessage_ReturnsCorrectMessage()
        {
            // Arrange & Act & Assert
            var studentIdAttr = new StudentIdAttribute();
            var message = studentIdAttr.FormatErrorMessage("StudentId");
            Assert.IsTrue(message.Contains("StudentId"), "Error message should contain field name");

            var batchYearAttr = new BatchYearAttribute();
            var yearMessage = batchYearAttr.FormatErrorMessage("BatchYear");
            Assert.IsTrue(yearMessage.Contains("BatchYear"), "Error message should contain field name");

            var examTitleAttr = new ExamTitleAttribute();
            var titleMessage = examTitleAttr.FormatErrorMessage("Title");
            Assert.IsTrue(titleMessage.Contains("Title"), "Error message should contain field name");
        }

        #endregion

        #region Edge Cases Tests

        [TestMethod]
        public void ValidationAttributes_WhitespaceValues_HandleCorrectly()
        {
            // Arrange
            var studentIdAttr = new StudentIdAttribute();
            var questionTextAttr = new QuestionTextAttribute();
            var examTitleAttr = new ExamTitleAttribute();

            // Act & Assert
            Assert.IsTrue(studentIdAttr.IsValid("   "), "Whitespace should return true (let Required handle it)");
            Assert.IsTrue(questionTextAttr.IsValid("   "), "Whitespace should return true (let Required handle it)");
            Assert.IsTrue(examTitleAttr.IsValid("   "), "Whitespace should return true (let Required handle it)");
        }

        [TestMethod]
        public void ValidationAttributes_BoundaryValues_HandleCorrectly()
        {
            // Arrange
            var batchYearAttr = new BatchYearAttribute(2000, 2050);
            var durationAttr = new ExamDurationAttribute(30, 480);

            // Act & Assert
            Assert.IsTrue(batchYearAttr.IsValid(2000), "Minimum boundary should be valid");
            Assert.IsTrue(batchYearAttr.IsValid(2050), "Maximum boundary should be valid");
            Assert.IsFalse(batchYearAttr.IsValid(1999), "Below minimum should be invalid");
            Assert.IsFalse(batchYearAttr.IsValid(2051), "Above maximum should be invalid");

            Assert.IsTrue(durationAttr.IsValid(30), "Minimum duration should be valid");
            Assert.IsTrue(durationAttr.IsValid(480), "Maximum duration should be valid");
            Assert.IsFalse(durationAttr.IsValid(29), "Below minimum duration should be invalid");
            Assert.IsFalse(durationAttr.IsValid(481), "Above maximum duration should be invalid");
        }

        #endregion
    }
}