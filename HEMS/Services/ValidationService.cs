using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using HEMS.Models;
using HEMS.Models.ViewModels;

namespace HEMS.Services
{
    public class ValidationService : IValidationService
    {
        private readonly HEMSContext _context;

        public ValidationService(HEMSContext context)
        {
            _context = context;
        }

        public HEMS.Models.ValidationResult ValidateStudentImport(StudentImportViewModel model)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (model == null)
            {
                errors.Add("Import model cannot be null");
                return new HEMS.Models.ValidationResult { IsValid = false, Errors = errors.ToArray() };
            }

            if (model.ImportFile == null)
            {
                errors.Add("File is required");
            }
            else
            {
                var fileValidation = ValidateFileUpload(model.ImportFile, new[] { ".csv", ".xlsx" }, 5 * 1024 * 1024);
                if (!fileValidation.IsValid)
                {
                    errors.AddRange(fileValidation.Errors);
                }
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateStudentData(Student student)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (student == null)
            {
                errors.Add("Student data cannot be null");
                return new HEMS.Models.ValidationResult { IsValid = false, Errors = errors.ToArray() };
            }

            if (string.IsNullOrWhiteSpace(student.Email))
            {
                errors.Add("Email is required");
            }
            else if (!IsValidEmail(student.Email))
            {
                errors.Add("Invalid email format");
            }

            if (string.IsNullOrWhiteSpace(student.FirstName))
            {
                errors.Add("First name is required");
            }

            if (string.IsNullOrWhiteSpace(student.LastName))
            {
                errors.Add("Last name is required");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateExamCreation(ExamCreateViewModel model)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (model == null)
            {
                errors.Add("Exam model cannot be null");
                return new HEMS.Models.ValidationResult { IsValid = false, Errors = errors.ToArray() };
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                errors.Add("Exam title is required");
            }

            if (model.DurationMinutes <= 0)
            {
                errors.Add("Exam duration must be greater than 0");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateQuestionCreation(QuestionCreateViewModel model)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (model == null)
            {
                errors.Add("Question model cannot be null");
                return new HEMS.Models.ValidationResult { IsValid = false, Errors = errors.ToArray() };
            }

            if (string.IsNullOrWhiteSpace(model.QuestionText))
            {
                errors.Add("Question text is required");
            }

            if (model.CorrectChoiceIndex < 0 || model.CorrectChoiceIndex > 3)
            {
                errors.Add("Correct choice index must be between 0 and 3");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateAuthenticationData(string email, string password)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("Email is required");
            }
            else if (!IsValidEmail(email))
            {
                errors.Add("Invalid email format");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateAuthenticationData(string email, string password, string confirmPassword)
        {
            var result = ValidateAuthenticationData(email, password);
            var errors = result.Errors.ToList();

            if (!string.IsNullOrWhiteSpace(password) && password != confirmPassword)
            {
                errors.Add("Passwords do not match");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = result.Warnings
            };
        }

        public HEMS.Models.ValidationResult ValidateSessionPassword(string password)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Session password is required");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateFileUpload(Microsoft.AspNetCore.Http.IFormFile file, string[] allowedExtensions, long maxSize)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (file == null)
            {
                errors.Add("File is required");
                return new HEMS.Models.ValidationResult { IsValid = false, Errors = errors.ToArray() };
            }

            if (file.Length > maxSize)
            {
                errors.Add($"File size exceeds maximum allowed size of {maxSize / (1024 * 1024)} MB");
            }

            var extension = Path.GetExtension(file.FileName)?.ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                errors.Add($"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        public HEMS.Models.ValidationResult ValidateModel<T>(T model) where T : class
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (model == null)
            {
                errors.Add("Model cannot be null");
                return new HEMS.Models.ValidationResult { IsValid = false, Errors = errors.ToArray() };
            }

            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
            {
                errors.AddRange(validationResults.Select(vr => vr.ErrorMessage));
            }

            return new HEMS.Models.ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}