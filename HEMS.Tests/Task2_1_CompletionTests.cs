using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Models;
using HEMS.Services;

namespace HEMS.Tests
{
    /// <summary>
    /// Comprehensive tests to verify Task 2.1 completion:
    /// Create Role and User models with authentication fields (loginPhaseCompleted, mustChangePassword)
    /// </summary>
    [TestClass]
    public class Task2_1_CompletionTests
    {
        private HEMSContext _context;
        private AuthenticationService _authService;

        [TestInitialize]
        public void Setup()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<HEMSContext>());
            _context = new HEMSContext();
            _context.Database.Initialize(true);
            _authService = new AuthenticationService(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _authService?.Dispose();
            _context?.Dispose();
        }

        [TestMethod]
        public void Task2_1_RoleModel_HasAllRequiredProperties()
        {
            // Arrange & Act
            var role = new Role
            {
                RoleName = "Student"
            };

            // Assert - Verify Role model has all required properties
            Assert.IsNotNull(role.RoleName, "Role should have RoleName property");
            Assert.IsNotNull(role.Users, "Role should have Users navigation property");
            Assert.AreEqual("Student", role.RoleName);
            
            // Verify the property can be set and retrieved
            role.RoleId = 1;
            Assert.AreEqual(1, role.RoleId);
        }

        [TestMethod]
        public void Task2_1_UserModel_HasAllRequiredAuthenticationFields()
        {
            // Arrange & Act
            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = 1
            };

            // Assert - Verify User model has all required properties including authentication fields
            Assert.IsNotNull(user.Username, "User should have Username property");
            Assert.IsNotNull(user.PasswordHash, "User should have PasswordHash property");
            Assert.IsTrue(user.RoleId > 0, "User should have RoleId property");
            
            // CRITICAL: Verify authentication fields exist and have correct defaults
            Assert.IsFalse(user.LoginPhaseCompleted, "LoginPhaseCompleted should default to false");
            Assert.IsTrue(user.MustChangePassword, "MustChangePassword should default to true");
            Assert.IsTrue(user.CreatedDate <= DateTime.Now, "CreatedDate should be set");

            // Verify authentication fields can be modified
            user.LoginPhaseCompleted = true;
            user.MustChangePassword = false;
            Assert.IsTrue(user.LoginPhaseCompleted, "LoginPhaseCompleted should be settable to true");
            Assert.IsFalse(user.MustChangePassword, "MustChangePassword should be settable to false");
        }

        [TestMethod]
        public void Task2_1_UserRoleRelationship_WorksCorrectly()
        {
            // Arrange
            var studentRole = new Role { RoleName = "Student" };
            var coordinatorRole = new Role { RoleName = "Coordinator" };
            
            _context.Roles.Add(studentRole);
            _context.Roles.Add(coordinatorRole);
            _context.SaveChanges();

            var studentUser = new User
            {
                Username = "student@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = studentRole.RoleId
            };

            var coordinatorUser = new User
            {
                Username = "coordinator@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = coordinatorRole.RoleId
            };

            _context.Users.Add(studentUser);
            _context.Users.Add(coordinatorUser);
            _context.SaveChanges();

            // Act
            var retrievedStudentUser = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == studentUser.UserId);

            var retrievedCoordinatorUser = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == coordinatorUser.UserId);

            // Assert
            Assert.IsNotNull(retrievedStudentUser, "Student user should be retrievable");
            Assert.IsNotNull(retrievedStudentUser.Role, "Student user should have role navigation property");
            Assert.AreEqual("Student", retrievedStudentUser.Role.RoleName, "Student user should have Student role");

            Assert.IsNotNull(retrievedCoordinatorUser, "Coordinator user should be retrievable");
            Assert.IsNotNull(retrievedCoordinatorUser.Role, "Coordinator user should have role navigation property");
            Assert.AreEqual("Coordinator", retrievedCoordinatorUser.Role.RoleName, "Coordinator user should have Coordinator role");
        }

        [TestMethod]
        public void Task2_1_AuthenticationService_UsesAuthenticationFields()
        {
            // Arrange
            var studentRole = new Role { RoleName = "Student" };
            _context.Roles.Add(studentRole);
            _context.SaveChanges();

            var user = new User
            {
                Username = "student@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = studentRole.RoleId,
                LoginPhaseCompleted = false,
                MustChangePassword = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act & Assert - Test Phase 1 completion
            Assert.IsFalse(_authService.IsPhase1Completed(user.UserId), "Phase 1 should not be completed initially");

            var phase1Completed = _authService.CompletePhase1Login(user.UserId);
            Assert.IsTrue(phase1Completed, "Phase 1 completion should succeed");
            Assert.IsTrue(_authService.IsPhase1Completed(user.UserId), "Phase 1 should be completed after calling CompletePhase1Login");

            // Verify the field was actually updated in the database
            var updatedUser = _context.Users.Find(user.UserId);
            Assert.IsTrue(updatedUser.LoginPhaseCompleted, "LoginPhaseCompleted should be true in database");

            // Act & Assert - Test password change
            var passwordChanged = _authService.ChangePassword(user.UserId, "newpassword");
            Assert.IsTrue(passwordChanged, "Password change should succeed");

            var userAfterPasswordChange = _context.Users.Find(user.UserId);
            Assert.IsFalse(userAfterPasswordChange.MustChangePassword, "MustChangePassword should be false after password change");
            Assert.AreNotEqual("hashedpassword", userAfterPasswordChange.PasswordHash, "Password hash should be updated");
        }

        [TestMethod]
        public void Task2_1_DatabaseSchema_SupportsAuthenticationFields()
        {
            // Arrange
            var role = new Role { RoleName = "TestRole" };
            _context.Roles.Add(role);
            _context.SaveChanges();

            // Act - Create user with specific authentication field values
            var user = new User
            {
                Username = "test@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = role.RoleId,
                LoginPhaseCompleted = true,
                MustChangePassword = false
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Clear context to ensure we're reading from database
            _context.Entry(user).Reload();

            // Assert - Verify authentication fields are persisted correctly
            Assert.IsTrue(user.LoginPhaseCompleted, "LoginPhaseCompleted should be persisted as true");
            Assert.IsFalse(user.MustChangePassword, "MustChangePassword should be persisted as false");

            // Test the opposite values
            user.LoginPhaseCompleted = false;
            user.MustChangePassword = true;
            _context.SaveChanges();
            _context.Entry(user).Reload();

            Assert.IsFalse(user.LoginPhaseCompleted, "LoginPhaseCompleted should be persisted as false");
            Assert.IsTrue(user.MustChangePassword, "MustChangePassword should be persisted as true");
        }

        [TestMethod]
        public void Task2_1_UserStudentRelationship_WorksWithAuthenticationFields()
        {
            // Arrange
            var studentRole = new Role { RoleName = "Student" };
            _context.Roles.Add(studentRole);
            _context.SaveChanges();

            var user = new User
            {
                Username = "student@university.edu",
                PasswordHash = "hashedpassword",
                RoleId = studentRole.RoleId,
                LoginPhaseCompleted = false,
                MustChangePassword = true
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
            var retrievedStudent = _context.Students
                .Include(s => s.User)
                .Include(s => s.User.Role)
                .FirstOrDefault(s => s.StudentId == student.StudentId);

            // Assert
            Assert.IsNotNull(retrievedStudent, "Student should be retrievable");
            Assert.IsNotNull(retrievedStudent.User, "Student should have User navigation property");
            Assert.IsFalse(retrievedStudent.User.LoginPhaseCompleted, "User's LoginPhaseCompleted should be accessible through Student");
            Assert.IsTrue(retrievedStudent.User.MustChangePassword, "User's MustChangePassword should be accessible through Student");
            Assert.AreEqual("Student", retrievedStudent.User.Role.RoleName, "User's Role should be accessible through Student");
        }

        [TestMethod]
        public void Task2_1_AuthenticationFieldsSupport_TwoPhaseAuthentication()
        {
            // This test verifies that the authentication fields support the two-phase authentication process
            // as described in the requirements

            // Arrange
            var studentRole = new Role { RoleName = "Student" };
            _context.Roles.Add(studentRole);
            _context.SaveChanges();

            var user = new User
            {
                Username = "student@university.edu",
                PasswordHash = "initialHash",
                RoleId = studentRole.RoleId
                // LoginPhaseCompleted defaults to false
                // MustChangePassword defaults to true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act & Assert - Phase 1: Identity Verification
            Assert.IsFalse(user.LoginPhaseCompleted, "Phase 1 should not be completed initially");
            
            // Simulate Phase 1 completion
            user.LoginPhaseCompleted = true;
            _context.SaveChanges();
            
            Assert.IsTrue(user.LoginPhaseCompleted, "Phase 1 should be completed");
            Assert.IsTrue(user.MustChangePassword, "Should still require password change for Phase 2");

            // Act & Assert - Phase 2: Exam Day Login with Password Change
            Assert.IsTrue(user.MustChangePassword, "Should require password change for Phase 2");
            
            // Simulate password change in Phase 2
            user.PasswordHash = "newHashAfterPhase2";
            user.MustChangePassword = false;
            _context.SaveChanges();

            Assert.IsTrue(user.LoginPhaseCompleted, "Phase 1 should remain completed");
            Assert.IsFalse(user.MustChangePassword, "Should not require password change after Phase 2 completion");
            Assert.AreEqual("newHashAfterPhase2", user.PasswordHash, "Password should be updated");

            // Final state: Both phases completed
            var finalUser = _context.Users.Find(user.UserId);
            Assert.IsTrue(finalUser.LoginPhaseCompleted, "Final state: Phase 1 completed");
            Assert.IsFalse(finalUser.MustChangePassword, "Final state: No password change required");
        }
    }
}