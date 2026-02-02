using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for university email addresses
    /// </summary>
    public class UniversityEmailAttribute : ValidationAttribute
    {
        private static readonly string[] ValidDomains = {
            "@hems.edu",
            "@edu.et",
            "@aau.edu.et",
            "@ju.edu.et",
            "@haramaya.edu.et",
            "@university.edu",
            "@university.edu.et"
        };

        public UniversityEmailAttribute() : base("Please enter a valid university email address.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string email = value.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Basic email format validation
            if (!IsValidEmailFormat(email))
                return false;

            // Check if email ends with any valid university domain
            foreach (var domain in ValidDomains)
            {
                if (email.EndsWith(domain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsValidEmailFormat(string email)
        {
            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be a valid university email address ending with one of the following domains: {string.Join(", ", ValidDomains)}.";
        }
    }
}