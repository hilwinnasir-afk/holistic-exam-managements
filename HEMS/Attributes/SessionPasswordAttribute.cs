using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for session passwords
    /// </summary>
    public class SessionPasswordAttribute : ValidationAttribute
    {
        public int MinLength { get; }
        public int MaxLength { get; }

        public SessionPasswordAttribute() : this(6, 20)
        {
        }

        public SessionPasswordAttribute(int minLength, int maxLength) : base("Please enter a valid session password.")
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string password = value.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Session password should be MinLength-MaxLength characters, alphanumeric
            var passwordRegex = new Regex($@"^[A-Za-z0-9]{{{MinLength},{MaxLength}}}$");
            return passwordRegex.IsMatch(password);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be {MinLength}-{MaxLength} alphanumeric characters.";
        }
    }
}