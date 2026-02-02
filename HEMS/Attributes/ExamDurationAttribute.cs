using System;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Attributes
{
    /// <summary>
    /// Validation attribute for exam duration in minutes
    /// </summary>
    public class ExamDurationAttribute : ValidationAttribute
    {
        public int MinMinutes { get; }
        public int MaxMinutes { get; }

        public ExamDurationAttribute(int minMinutes, int maxMinutes) : base("Please enter a valid exam duration.")
        {
            MinMinutes = minMinutes;
            MaxMinutes = maxMinutes;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            if (int.TryParse(value.ToString(), out int minutes))
            {
                return minutes >= MinMinutes && minutes <= MaxMinutes;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be between {MinMinutes} and {MaxMinutes} minutes.";
        }
    }
}