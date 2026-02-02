using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for student ID numbers
    /// </summary>
    public class StudentIdAttribute : ValidationAttribute
    {
        public StudentIdAttribute() : base("Please enter a valid student ID.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string studentId = value.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(studentId))
                return false;

            // Student ID should be alphanumeric and between 3-20 characters
            var studentIdRegex = new Regex(@"^[A-Za-z0-9]{3,20}$");
            return studentIdRegex.IsMatch(studentId);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be a valid student ID (3-20 alphanumeric characters).";
        }
    }
}