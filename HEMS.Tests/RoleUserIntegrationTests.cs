using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Models;

namespace HEMS.Tests
{
    [TestClass]
    public class RoleUserIntegrationTests
    {
        private HEMSContext _context;

        [TestInitialize]
        public void Setup()
        {
            // Use in-memory database for testing
            Database.SetInitializer(new DropCreateDatabaseAlways<HEMSContext>());
            _context = new HEMSContext();
            _context.Database.Initialize(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        [TestMethod]
        public void Role_CanBeCreatedAndRetrieved()
        {
            // Arrange
            var role = new Role
            {
                RoleName = "Student"
            };

            // Act
            _context.Roles.Add(role);
            _context.SaveChanges();

            var retrievedRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Student");

            // Assert
            Assert.IsNotNull(retrievedRole);
            Assert.AreEqual("Student", retrievedRole.RoleName);
            Assert.IsTrue(retrievedRole.RoleId > 0);
        }

        [TestMethod]
        public void User_CanBeCreatedWithDefaultAuthenticationFields()
        {
            // Arrange
            var role = new Role { RoleName = "Student" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };

            // Act
            _context.Users.Add(user);
            _context.SaveChanges();

            var retrievedUser = _context.Users.FirstOrDefault(u => u.Username == "test@university.edu");

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.AreEqual("test@university.edu", retrievedUser.Username);
            Assert.AreEqual("hashedpassword", retrievedUser.PasswordHash);
            Assert.AreEqual(role.RoleId, retrievedUser.RoleId);
            Assert.IsFalse(retrievedUser.LoginPhaseCompleted); // Default should be false
            Assert.IsTrue(retrievedUser.MustChangePassword); // Default should be true
            Assert.IsTrue(retrievedUser.CreatedDate <= DateTime.Now);
        }

        [TestMethod]
        public void User_AuthenticationFields_CanBeUpdated()
        {
            // Arrange
            var role = new Role { RoleName = "Student" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act - Update authentication fields
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            _context.SaveChanges();

            var retrievedUser = _context.Users.FirstOrDefault(u => u.UserId == user.UserId);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.IsTrue(retrievedUser.LoginPhaseCompleted);
            Assert.IsFalse(retrievedUser.MustChangePassword);
        }

        [TestMethod]
        public void User_Role_NavigationProperty_WorksCorrectly()
        {
            // Arrange
            var role = new Role { RoleName = "Coordinator" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = "coordinator@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var retrievedUser = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == user.UserId);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.IsNotNull(retrievedUser.Role);
            Assert.AreEqual("Coordinator", retrievedUser.Role.RoleName);
        }

        [TestMethod]
        public void Student_User_Relationship_WorksCorrectly()
        {
            // Arrange
            var role = new Role { RoleName = "Student" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            var user = new User
            {
                Username = "student@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var student = new Student
            {
                UserId = user.UserId,
                IdNumber = "SE123",
                UniversityEmail = "student@university.edu",
                BatchYear = 2024
            };
            _context.Students.Add(student);
            _context.SaveChanges();

            // Act
            var retrievedUser = _context.Users
                .Include(u => u.Student)
                .FirstOrDefault(u => u.UserId == user.UserId);

            var retrievedStudent = _context.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == student.StudentId);

            // Assert
            Assert.IsNotNull(retrievedUser);
            Assert.IsNotNull(retrievedUser.Student);
            Assert.AreEqual("SE123", retrievedUser.Student.IdNumber);
            Assert.AreEqual("student@university.edu", retrievedUser.Student.UniversityEmail);
            Assert.AreEqual(2024, retrievedUser.Student.BatchYear);

            Assert.IsNotNull(retrievedStudent);
            Assert.IsNotNull(retrievedStudent.User);
            Assert.AreEqual("student@university.edu", retrievedStudent.User.Username);
        }

        [TestMethod]
        public void Multiple_Users_CanHaveSameRole()
        {
            // Arrange
            var studentRole = new Role { RoleName = "Student" };
            _context.Roles.Add(studentRole);
            _context.SaveChanges();

            var user1 = new User
            {
                Username = "student1@university.edu",
                PasswordHash = "hashedpassword1",
                RoleId = studentRole.RoleId
            };

            var user2 = new User
            {
                Username = "student2@university.edu",
                PasswordHash = "hashedpassword2",
                RoleId = studentRole.RoleId
            };

            // Act
            _context.Users.Add(user1);
            _context.Users.Add(user2);
            _context.SaveChanges();

            var studentsWithRole = _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Student")
                .ToList();

            // Assert
            Assert.AreEqual(2, studentsWithRole.Count);
            Assert.IsTrue(studentsWithRole.All(u => u.Role.RoleName == "Student"));
        }
    }
}