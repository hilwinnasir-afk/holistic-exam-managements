using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for question text
    /// </summary>
    public class QuestionTextAttribute : ValidationAttribute
    {
        public int MinLength { get; }
        public int MaxLength { get; }

        public QuestionTextAttribute() : this(10, 1000)
        {
        }

        public QuestionTextAttribute(int minLength, int maxLength) : base("Please enter a valid question text.")
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string questionText = value.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(questionText))
                return false;

            // Question text should be between MinLength-MaxLength characters
            return questionText.Trim().Length >= MinLength && questionText.Trim().Length <= MaxLength;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be between {MinLength} and {MaxLength} characters.";
        }
    }
}