using System;
using System.Web;
using HEMS.Models;
using HEMS.Models.ViewModels;

namespace HEMS.Services
{
    public interface IValidationService : IDisposable
    {
        HEMS.Models.ValidationResult ValidateStudentImport(StudentImportViewModel model);
        HEMS.Models.ValidationResult ValidateStudentData(Student student);
        HEMS.Models.ValidationResult ValidateExamCreation(ExamCreateViewModel model);
        HEMS.Models.ValidationResult ValidateQuestionCreation(QuestionCreateViewModel model);
        HEMS.Models.ValidationResult ValidateAuthenticationData(string email, string password);
        HEMS.Models.ValidationResult ValidateAuthenticationData(string email, string password, string confirmPassword);
        HEMS.Models.ValidationResult ValidateSessionPassword(string password);
        HEMS.Models.ValidationResult ValidateFileUpload(Microsoft.AspNetCore.Http.IFormFile file, string[] allowedExtensions, long maxSize);
        HEMS.Models.ValidationResult ValidateModel<T>(T model) where T : class;
    }
}