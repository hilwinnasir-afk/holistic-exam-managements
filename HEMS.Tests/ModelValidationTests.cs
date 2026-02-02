using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Models;

namespace HEMS.Tests
{
    [TestClass]
    public class ModelValidationTests
    {
        private ValidationContext GetValidationContext(object model)
        {
            return new ValidationContext(model, null, null);
        }

        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = GetValidationContext(model);
            Validator.TryValidateObject(model, context, validationResults, true);
            return validationResults;
        }

        [TestMethod]
        public void Role_RequiredFields_ValidationWorks()
        {
            // Arrange - Valid role
            var validRole = new Role
            {
                RoleName = "Student"
            };

            // Act
            var validationResults = ValidateModel(validRole);

            // Assert
            Assert.AreEqual(0, validationResults.Count, "Valid role should have no validation errors");

            // Arrange - Invalid role (missing required field)
            var invalidRole = new Role
            {
                RoleName = null
            };

            // Act
            var invalidValidationResults = ValidateModel(invalidRole);

            // Assert
            Assert.IsTrue(invalidValidationResults.Count > 0, "Invalid role should have validation errors");
            Assert.IsTrue(invalidValidationResults.Any(vr => vr.MemberNames.Contains("RoleName")), 
                "Should have validation error for RoleName");
        }

        [TestMethod]
        public void User_RequiredFields_ValidationWorks()
        {
            // Arrange - Valid user
            var validUser = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Act
            var validationResults = ValidateModel(validUser);

            // Assert
            Assert.AreEqual(0, validationResults.Count, "Valid user should have no validation errors");

            // Arrange - Invalid user (missing required fields)
            var invalidUser = new User
            {
                Username = null,
                PasswordHash = null,
                RoleId = 0
            };

            // Act
            var invalidValidationResults = ValidateModel(invalidUser);

            // Assert
            Assert.IsTrue(invalidValidationResults.Count > 0, "Invalid user should have validation errors");
        }

        [TestMethod]
        public void User_AuthenticationFields_HaveCorrectDefaults()
        {
            // Arrange & Act
            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Assert
            Assert.IsFalse(user.LoginPhaseCompleted, "LoginPhaseCompleted should default to false");
            Assert.IsTrue(user.MustChangePassword, "MustChangePassword should default to true");
            Assert.IsTrue(user.CreatedDate <= DateTime.Now, "CreatedDate should be set to current time or earlier");
        }

        [TestMethod]
        public void Student_RequiredFields_ValidationWorks()
        {
            // Arrange - Valid student
            var validStudent = new Student
            {
                UserId = 1,
                IdNumber = "SE123",
                UniversityEmail = "student@university.edu",
                BatchYear = 2024
            };

            // Act
            var validationResults = ValidateModel(validStudent);

            // Assert
            Assert.AreEqual(0, validationResults.Count, "Valid student should have no validation errors");

            // Arrange - Invalid student (missing required fields)
            var invalidStudent = new Student
            {
                UserId = 0,
                IdNumber = null,
                UniversityEmail = null,
                BatchYear = 0
            };

            // Act
            var invalidValidationResults = ValidateModel(invalidStudent);

            // Assert
            Assert.IsTrue(invalidValidationResults.Count > 0, "Invalid student should have validation errors");
        }

        [TestMethod]
        public void Student_StringLength_ValidationWorks()
        {
            // Arrange - Student with string too long
            var studentWithLongId = new Student
            {
                UserId = 1,
                IdNumber = new string('A', 25), // Exceeds 20 character limit
                UniversityEmail = "student@university.edu",
                BatchYear = 2024
            };

            // Act
            var validationResults = ValidateModel(studentWithLongId);

            // Assert
            Assert.IsTrue(validationResults.Any(vr => vr.MemberNames.Contains("IdNumber")), 
                "Should have validation error for IdNumber length");

            // Arrange - Student with email too long
            var studentWithLongEmail = new Student
            {
                UserId = 1,
                IdNumber = "SE123",
                UniversityEmail = new string('a', 95) + "@university.edu", // Exceeds 100 character limit
                BatchYear = 2024
            };

            // Act
            var emailValidationResults = ValidateModel(studentWithLongEmail);

            // Assert
            Assert.IsTrue(emailValidationResults.Any(vr => vr.MemberNames.Contains("UniversityEmail")), 
                "Should have validation error for UniversityEmail length");
        }

        [TestMethod]
        public void User_StringLength_ValidationWorks()
        {
            // Arrange - User with username too long
            var userWithLongUsername = new User
            {
                Username = new string('a', 95) + "@university.edu", // Exceeds 100 character limit
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Act
            var validationResults = ValidateModel(userWithLongUsername);

            // Assert
            Assert.IsTrue(validationResults.Any(vr => vr.MemberNames.Contains("Username")), 
                "Should have validation error for Username length");

            // Arrange - User with password hash too long
            var userWithLongPassword = new User
            {
                Username = "test@university.edu",
                PasswordHash = new string('a', 260), // Exceeds 255 character limit
                RoleId = 1
            };

            // Act
            var passwordValidationResults = ValidateModel(userWithLongPassword);

            // Assert
            Assert.IsTrue(passwordValidationResults.Any(vr => vr.MemberNames.Contains("PasswordHash")), 
                "Should have validation error for PasswordHash length");
        }

        [TestMethod]
        public void Role_StringLength_ValidationWorks()
        {
            // Arrange - Role with name too long
            var roleWithLongName = new Role
            {
                RoleName = new string('A', 55) // Exceeds 50 character limit
            };

            // Act
            var validationResults = ValidateModel(roleWithLongName);

            // Assert
            Assert.IsTrue(validationResults.Any(vr => vr.MemberNames.Contains("RoleName")), 
                "Should have validation error for RoleName length");
        }

        [TestMethod]
        public void Models_NavigationProperties_InitializeCorrectly()
        {
            // Arrange & Act
            var role = new Role();
            var student = new Student();

            // Assert
            Assert.IsNotNull(role.Users, "Role.Users collection should be initialized");
            Assert.IsNotNull(student.StudentExams, "Student.StudentExams collection should be initialized");
            Assert.AreEqual(0, role.Users.Count, "Role.Users should be empty initially");
            Assert.AreEqual(0, student.StudentExams.Count, "Student.StudentExams should be empty initially");
        }
    }
}