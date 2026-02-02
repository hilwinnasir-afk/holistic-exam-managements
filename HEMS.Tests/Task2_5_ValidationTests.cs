using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HEMS.Services;
using HEMS.Models;
using HEMS.Models.ViewModels;
using HEMS.Attributes;

namespace HEMS.Tests
{
    /// <summary>
    /// Tests for Task 2.5: Add university email domain validation
    /// Validates that university email domain validation is properly implemented
    /// across the application layers (service, model, view model)
    /// </summary>
    [TestClass]
    public class Task2_5_ValidationTests
    {
        private AuthenticationService _authService;

        [TestInitialize]
        public void Setup()
        {
            _authService = new AuthenticationService();
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify that AuthenticationService has comprehensive email domain validation
        /// Tests the enhanced email validation with additional Ethiopian universities
        /// </summary>
        [TestMethod]
        public void Task2_5_AuthenticationService_HasEnhancedEmailValidation()
        {
            // Test original domains still work
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@hems.edu"), 
                "Should accept hems.edu domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@edu.et"), 
                "Should accept Ethiopian educational domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@aau.edu.et"), 
                "Should accept Addis Ababa University domain");

            // Test new Ethiopian university domains
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@hawassa.edu.et"), 
                "Should accept Hawassa University domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@arba-minch.edu.et"), 
                "Should accept Arba Minch University domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@dmu.edu.et"), 
                "Should accept Debre Markos University domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@gmu.edu.et"), 
                "Should accept Gondar University domain");
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@kmu.edu.et"), 
                "Should accept Kotebe Metropolitan University domain");

            // Test that invalid domains are still rejected
            Assert.IsFalse(_authService.IsUniversityEmailValid("student@gmail.com"), 
                "Should reject non-university domains");
            Assert.IsFalse(_authService.IsUniversityEmailValid("student@yahoo.com"), 
                "Should reject non-university domains");
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify that UniversityEmailAttribute validation works correctly
        /// Tests the custom validation attribute for university email domains
        /// </summary>
        [TestMethod]
        public void Task2_5_UniversityEmailAttribute_ValidatesCorrectly()
        {
            var attribute = new UniversityEmailAttribute();

            // Test valid university emails
            Assert.IsTrue(attribute.IsValid("student@hems.edu"), 
                "Should accept valid university email");
            Assert.IsTrue(attribute.IsValid("student@edu.et"), 
                "Should accept Ethiopian educational domain");
            Assert.IsTrue(attribute.IsValid("student@hawassa.edu.et"), 
                "Should accept Hawassa University");

            // Test invalid emails
            Assert.IsFalse(attribute.IsValid("student@gmail.com"), 
                "Should reject non-university email");
            Assert.IsFalse(attribute.IsValid("invalid-email"), 
                "Should reject invalid email format");

            // Test edge cases
            Assert.IsTrue(attribute.IsValid(null), 
                "Should return true for null to let [Required] handle it");
            Assert.IsTrue(attribute.IsValid(""), 
                "Should return true for empty to let [Required] handle it");
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify that Phase1LoginViewModel has university email validation
        /// Tests that the view model properly validates university email domains
        /// </summary>
        [TestMethod]
        public void Task2_5_Phase1LoginViewModel_HasUniversityEmailValidation()
        {
            // Test valid university email
            var validModel = new Phase1LoginViewModel
            {
                Email = "student@hems.edu",
                Password = "SE12319"
            };

            var validResults = ValidateModel(validModel);
            var emailErrors = validResults.Where(vr => vr.MemberNames.Contains("Email")).ToList();
            
            Assert.AreEqual(0, emailErrors.Count, 
                "Valid university email should not have validation errors");

            // Test invalid email domain
            var invalidModel = new Phase1LoginViewModel
            {
                Email = "student@gmail.com",
                Password = "SE12319"
            };

            var invalidResults = ValidateModel(invalidModel);
            var invalidEmailErrors = invalidResults.Where(vr => vr.MemberNames.Contains("Email")).ToList();
            
            Assert.IsTrue(invalidEmailErrors.Count > 0, 
                "Invalid university email should have validation errors");
            Assert.IsTrue(invalidEmailErrors.Any(e => e.ErrorMessage.Contains("university email")), 
                "Should have university email validation error message");
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify that Student model has university email validation
        /// Tests that the Student model properly validates university email domains
        /// </summary>
        [TestMethod]
        public void Task2_5_StudentModel_HasUniversityEmailValidation()
        {
            // Test valid university email
            var validStudent = new Student
            {
                IdNumber = "SE123",
                UniversityEmail = "student@aau.edu.et",
                BatchYear = 2024
            };

            var validResults = ValidateModel(validStudent);
            var emailErrors = validResults.Where(vr => vr.MemberNames.Contains("UniversityEmail")).ToList();
            
            Assert.AreEqual(0, emailErrors.Count, 
                "Valid university email should not have validation errors");

            // Test invalid email domain
            var invalidStudent = new Student
            {
                IdNumber = "SE123",
                UniversityEmail = "student@gmail.com",
                BatchYear = 2024
            };

            var invalidResults = ValidateModel(invalidStudent);
            var invalidEmailErrors = invalidResults.Where(vr => vr.MemberNames.Contains("UniversityEmail")).ToList();
            
            Assert.IsTrue(invalidEmailErrors.Count > 0, 
                "Invalid university email should have validation errors");
            Assert.IsTrue(invalidEmailErrors.Any(e => e.ErrorMessage.Contains("university email") || 
                                                     e.ErrorMessage.Contains("educational institution")), 
                "Should have university email validation error message");
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify case-insensitive email validation
        /// Tests that email validation works regardless of case
        /// </summary>
        [TestMethod]
        public void Task2_5_EmailValidation_IsCaseInsensitive()
        {
            var attribute = new UniversityEmailAttribute();

            // Test different case variations
            Assert.IsTrue(attribute.IsValid("student@hems.edu"), "Lowercase should be valid");
            Assert.IsTrue(attribute.IsValid("STUDENT@HEMS.EDU"), "Uppercase should be valid");
            Assert.IsTrue(attribute.IsValid("Student@HEMS.edu"), "Mixed case should be valid");
            Assert.IsTrue(attribute.IsValid("student@HAWASSA.EDU.ET"), "Ethiopian domain uppercase should be valid");

            // Verify service layer is also case-insensitive
            Assert.IsTrue(_authService.IsUniversityEmailValid("student@hems.edu"), "Service: lowercase should be valid");
            Assert.IsTrue(_authService.IsUniversityEmailValid("STUDENT@HEMS.EDU"), "Service: uppercase should be valid");
            Assert.IsTrue(_authService.IsUniversityEmailValid("Student@HAWASSA.edu.et"), "Service: mixed case should be valid");
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify comprehensive Ethiopian university domain support
        /// Tests that all major Ethiopian universities are supported
        /// </summary>
        [TestMethod]
        public void Task2_5_EthiopianUniversities_AllSupported()
        {
            var ethiopianUniversities = new[]
            {
                "student@edu.et",           // General Ethiopian educational domain
                "student@aau.edu.et",       // Addis Ababa University
                "student@ju.edu.et",        // Jimma University
                "student@mu.edu.et",        // Mekelle University
                "student@hu.edu.et",        // Haramaya University
                "student@bdu.edu.et",       // Bahir Dar University
                "student@dmu.edu.et",       // Debre Markos University
                "student@wku.edu.et",       // Wollo University
                "student@gmu.edu.et",       // Gondar University
                "student@hawassa.edu.et",   // Hawassa University
                "student@arba-minch.edu.et", // Arba Minch University
                "student@dbu.edu.et",       // Debre Berhan University
                "student@wsu.edu.et",       // Wachemo University
                "student@amu.edu.et",       // Adama Science and Technology University
                "student@bonga.edu.et",     // Bonga University
                "student@ddu.edu.et",       // Dire Dawa University
                "student@kmu.edu.et"        // Kotebe Metropolitan University
            };

            foreach (var email in ethiopianUniversities)
            {
                Assert.IsTrue(_authService.IsUniversityEmailValid(email), 
                    $"Ethiopian university email {email} should be valid");
                
                var attribute = new UniversityEmailAttribute();
                Assert.IsTrue(attribute.IsValid(email), 
                    $"UniversityEmailAttribute should accept {email}");
            }
        }

        /// <summary>
        /// Task 2.5 Requirement: Verify that only imported student emails are allowed
        /// Tests that even with valid university domain, email must exist in student records
        /// </summary>
        [TestMethod]
        public void Task2_5_EmailValidation_OnlyAllowsImportedStudentEmails()
        {
            // This test verifies that the system validates against imported student data
            // Even if an email has a valid university domain, it must exist in the student records
            
            // The current implementation already does this correctly in ValidatePhase1Login:
            // 1. First validates domain format with IsUniversityEmailValid()
            // 2. Then checks if user exists in database with GetUserByEmail()
            // 3. Then verifies student record exists
            
            // This ensures that only coordinator-imported students can authenticate
            
            var validDomainButNotImported = "notimported@hems.edu";
            
            // Even though domain is valid, this should fail because user doesn't exist in database
            Assert.IsTrue(_authService.IsUniversityEmailValid(validDomainButNotImported), 
                "Domain validation should pass for valid university domain");
            
            // But Phase 1 login should fail because user is not in imported student records
            Assert.IsFalse(_authService.ValidatePhase1Login(validDomainButNotImported, "anypassword"), 
                "Phase 1 login should fail for non-imported student even with valid domain");
            
            // This demonstrates that the system correctly validates against imported student data
            // The GetUserByEmail() method returns null for non-imported students
            var nonImportedUser = _authService.GetUserByEmail(validDomainButNotImported);
            Assert.IsNull(nonImportedUser, 
                "GetUserByEmail should return null for non-imported students");
        }

        /// <summary>
        /// Helper method to validate a model using data annotations
        /// </summary>
        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _authService?.Dispose();
        }
    }
}