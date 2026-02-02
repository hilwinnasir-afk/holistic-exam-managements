using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using HEMS.Attributes;

namespace HEMS.Models.ViewModels
{
    /// <summary>
    /// View model for importing students via CSV/Excel upload
    /// </summary>
    public class StudentImportViewModel
    {
        [Required(ErrorMessage = "Please select a file to upload")]
        [Display(Name = "Student Data File")]
        public IFormFile ImportFile { get; set; }

        [Display(Name = "Batch Year")]
        [Required(ErrorMessage = "Batch year is required")]
        [StringLength(50, ErrorMessage = "Batch Year cannot exceed 50 characters")]
        public string BatchYear { get; set; }

        [Display(Name = "Skip Header Row")]
        public bool SkipHeaderRow { get; set; } = true;

        public List<StudentImportResult> ImportResults { get; set; }
        public List<string> ValidationErrors { get; set; }
        public List<string> ValidationWarnings { get; set; }

        public StudentImportViewModel()
        {
            ImportResults = new List<StudentImportResult>();
            ValidationErrors = new List<string>();
            ValidationWarnings = new List<string>();
        }
    }

    /// <summary>
    /// Individual student data for import
    /// </summary>
    public class StudentImportModel
    {
        [Required(ErrorMessage = "Student Name is required")]
        [StringLength(100, ErrorMessage = "Student Name cannot exceed 100 characters")]
        public string StudentName { get; set; }

        [Required(ErrorMessage = "ID Number is required")]
        [StringLength(20, ErrorMessage = "ID Number cannot exceed 20 characters")]
        [StudentId]
        public string IdNumber { get; set; }

        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
        public string Gender { get; set; }

        [StringLength(10, ErrorMessage = "Section cannot exceed 10 characters")]
        public string Section { get; set; }

        [Required(ErrorMessage = "University Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [UniversityEmail(ErrorMessage = "University email must be from an approved educational institution")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string UniversityEmail { get; set; }

        [Required(ErrorMessage = "Batch Year is required")]
        [StringLength(50, ErrorMessage = "Batch Year cannot exceed 50 characters")]
        public string BatchYear { get; set; }
    }

    /// <summary>
    /// Result of importing a single student record
    /// </summary>
    public class StudentImportResult
    {
        public string StudentName { get; set; }
        public string IdNumber { get; set; }
        public string Gender { get; set; }
        public string Section { get; set; }
        public string UniversityEmail { get; set; }
        public string BatchYear { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int? CreatedUserId { get; set; }
        public int? CreatedStudentId { get; set; }
    }

    /// <summary>
    /// Summary of import operation
    /// </summary>
    public class ImportSummaryViewModel
    {
        public int TotalRecords { get; set; }
        public int SuccessfulImports { get; set; }
        public int FailedImports { get; set; }
        public List<StudentImportResult> Results { get; set; }
        public DateTime ImportDate { get; set; }

        public ImportSummaryViewModel()
        {
            Results = new List<StudentImportResult>();
            ImportDate = DateTime.Now;
        }
    }
}