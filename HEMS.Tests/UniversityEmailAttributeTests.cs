using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Attributes;

namespace HEMS.Tests
{
    [TestClass]
    public class UniversityEmailAttributeTests
    {
        private UniversityEmailAttribute _attribute;

        [TestInitialize]
        public void Setup()
        {
            _attribute = new UniversityEmailAttribute();
        }

        [TestMethod]
        public void UniversityEmailAttribute_ValidUniversityEmail_ReturnsTrue()
        {
            // Arrange
            string email = "student@hems.edu";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_EthiopianEducationalDomain_ReturnsTrue()
        {
            // Arrange
            string email = "student@edu.et";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_AddisAbabaUniversity_ReturnsTrue()
        {
            // Arrange
            string email = "student@aau.edu.et";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_JimmaUniversity_ReturnsTrue()
        {
            // Arrange
            string email = "student@ju.edu.et";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_HawassaUniversity_ReturnsTrue()
        {
            // Arrange
            string email = "student@hawassa.edu.et";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_ArbaMinchUniversity_ReturnsTrue()
        {
            // Arrange
            string email = "student@arba-minch.edu.et";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_InvalidDomain_ReturnsFalse()
        {
            // Arrange
            string email = "student@gmail.com";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_InvalidEmailFormat_ReturnsFalse()
        {
            // Arrange
            string email = "invalid-email-format";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_EmptyEmail_ReturnsTrue()
        {
            // Arrange - Empty should return true to let [Required] handle it
            string email = "";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_NullEmail_ReturnsTrue()
        {
            // Arrange - Null should return true to let [Required] handle it
            string email = null;

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            string emailLower = "student@hems.edu";
            string emailUpper = "STUDENT@HEMS.EDU";
            string emailMixed = "Student@HEMS.edu";

            // Act
            bool resultLower = _attribute.IsValid(emailLower);
            bool resultUpper = _attribute.IsValid(emailUpper);
            bool resultMixed = _attribute.IsValid(emailMixed);

            // Assert
            Assert.IsTrue(resultLower);
            Assert.IsTrue(resultUpper);
            Assert.IsTrue(resultMixed);
        }

        [TestMethod]
        public void UniversityEmailAttribute_MissingAtSymbol_ReturnsFalse()
        {
            // Arrange
            string email = "studenthems.edu";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_MultipleAtSymbols_ReturnsFalse()
        {
            // Arrange
            string email = "student@@hems.edu";

            // Act
            bool result = _attribute.IsValid(email);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void UniversityEmailAttribute_FormatErrorMessage_ReturnsCorrectMessage()
        {
            // Arrange
            string fieldName = "Email";

            // Act
            string message = _attribute.FormatErrorMessage(fieldName);

            // Assert
            Assert.IsTrue(message.Contains("valid university email address"));
        }

        [TestMethod]
        public void UniversityEmailAttribute_AllEthiopianUniversities_ReturnsTrue()
        {
            // Arrange - Test all Ethiopian university domains
            string[] ethiopianEmails = {
                "student@edu.et",
                "student@aau.edu.et",
                "student@ju.edu.et",
                "student@mu.edu.et",
                "student@hu.edu.et",
                "student@bdu.edu.et",
                "student@dmu.edu.et",
                "student@wku.edu.et",
                "student@gmu.edu.et",
                "student@hawassa.edu.et",
                "student@arba-minch.edu.et",
                "student@dbu.edu.et",
                "student@wsu.edu.et",
                "student@amu.edu.et",
                "student@bonga.edu.et",
                "student@ddu.edu.et",
                "student@kmu.edu.et"
            };

            // Act & Assert
            foreach (string email in ethiopianEmails)
            {
                bool result = _attribute.IsValid(email);
                Assert.IsTrue(result, $"Email {email} should be valid");
            }
        }
    }
}