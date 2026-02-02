using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Models;

namespace HEMS.Tests
{
    [TestClass]
    public class ModelTests
    {
        [TestMethod]
        public void Role_Creation_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var role = new Role
            {
                RoleId = 1,
                RoleName = "Student"
            };

            // Assert
            Assert.AreEqual(1, role.RoleId);
            Assert.AreEqual("Student", role.RoleName);
            Assert.IsNotNull(role.Users);
        }

        [TestMethod]
        public void User_Creation_SetsDefaultValuesCorrectly()
        {
            // Arrange & Act
            var user = new User
            {
                UserId = 1,
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Assert
            Assert.AreEqual(1, user.UserId);
            Assert.AreEqual("test@university.edu", user.Username);
            Assert.AreEqual("hashedpassword", user.PasswordHash);
            Assert.AreEqual(1, user.RoleId);
            Assert.IsFalse(user.LoginPhaseCompleted); // Should default to false
            Assert.IsTrue(user.MustChangePassword); // Should default to true
            Assert.IsTrue(user.CreatedDate <= DateTime.Now);
        }

        [TestMethod]
        public void User_LoginPhaseCompleted_CanBeSetToTrue()
        {
            // Arrange
            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Act
            user.LoginPhaseCompleted = true;

            // Assert
            Assert.IsTrue(user.LoginPhaseCompleted);
        }

        [TestMethod]
        public void User_MustChangePassword_CanBeSetToFalse()
        {
            // Arrange
            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Act
            user.MustChangePassword = false;

            // Assert
            Assert.IsFalse(user.MustChangePassword);
        }

        [TestMethod]
        public void Student_Creation_SetsPropertiesCorrectly()
        {
            // Arrange & Act
            var student = new Student
            {
                StudentId = 1,
                UserId = 1,
                IdNumber = "SE123",
                UniversityEmail = "student@university.edu",
                BatchYear = 2024
            };

            // Assert
            Assert.AreEqual(1, student.StudentId);
            Assert.AreEqual(1, student.UserId);
            Assert.AreEqual("SE123", student.IdNumber);
            Assert.AreEqual("student@university.edu", student.UniversityEmail);
            Assert.AreEqual(2024, student.BatchYear);
            Assert.IsTrue(student.CreatedDate <= DateTime.Now);
            Assert.IsNotNull(student.StudentExams);
        }

        [TestMethod]
        public void User_Student_NavigationProperty_CanBeSet()
        {
            // Arrange
            var user = new User
            {
                UserId = 1,
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            var student = new Student
            {
                StudentId = 1,
                UserId = 1,
                IdNumber = "SE123",
                UniversityEmail = "test@university.edu",
                BatchYear = 2024
            };

            // Act
            user.Student = student;
            student.User = user;

            // Assert
            Assert.AreEqual(student, user.Student);
            Assert.AreEqual(user, student.User);
        }
    }
}