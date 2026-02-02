using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for choice text
    /// </summary>
    public class ChoiceTextAttribute : ValidationAttribute
    {
        public int MinLength { get; }
        public int MaxLength { get; }

        public ChoiceTextAttribute() : this(1, 500)
        {
        }

        public ChoiceTextAttribute(int minLength, int maxLength) : base("Please enter valid choice text.")
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string choiceText = value.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(choiceText))
                return false;

            // Choice text should be between MinLength-MaxLength characters
            return choiceText.Trim().Length >= MinLength && choiceText.Trim().Length <= MaxLength;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be between {MinLength} and {MaxLength} characters.";
        }
    }
}