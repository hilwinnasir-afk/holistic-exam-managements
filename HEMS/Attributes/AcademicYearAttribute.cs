using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for academic year values
    /// </summary>
    public class AcademicYearAttribute : ValidationAttribute
    {
        public int MinYear { get; }
        public int MaxYear { get; }

        public AcademicYearAttribute() : this(2000, 2050)
        {
        }

        public AcademicYearAttribute(int minYear, int maxYear) : base("Please enter a valid academic year.")
        {
            MinYear = minYear;
            MaxYear = maxYear;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            if (int.TryParse(value.ToString(), out int year))
            {
                return year >= MinYear && year <= MaxYear;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be between {MinYear} and {MaxYear}.";
        }
    }
}