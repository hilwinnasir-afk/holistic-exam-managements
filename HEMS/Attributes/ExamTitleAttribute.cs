using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for exam titles
    /// </summary>
    public class ExamTitleAttribute : ValidationAttribute
    {
        public int MinLength { get; }
        public int MaxLength { get; }

        public ExamTitleAttribute() : this(5, 200)
        {
        }

        public ExamTitleAttribute(int minLength, int maxLength) : base("Please enter a valid exam title.")
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string title = value.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(title))
                return false;

            // Title should be between MinLength-MaxLength characters
            return title.Trim().Length >= MinLength && title.Trim().Length <= MaxLength;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be between {MinLength} and {MaxLength} characters.";
        }
    }
}