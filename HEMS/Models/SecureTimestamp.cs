using System;

namespace HEMS.Models
{
    /// <summary>
    /// Represents a secure timestamp for exam timer validation
    /// </summary>
    public class SecureTimestamp
    {
        /// <summary>
        /// Gets or sets the current server time
        /// </summary>
        public DateTime ServerTime { get; set; }

        /// <summary>
        /// Gets or sets the exam start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the exam end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the remaining time for the exam
        /// </summary>
        public TimeSpan RemainingTime { get; set; }

        /// <summary>
        /// Gets or sets whether the exam has expired
        /// </summary>
        public bool IsExpired { get; set; }

        /// <summary>
        /// Gets or sets the security hash for validation
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Gets the remaining time formatted as a string
        /// </summary>
        public string FormattedRemainingTime
        {
            get
            {
                if (RemainingTime <= TimeSpan.Zero)
                {
                    return "00:00:00";
                }

                return string.Format("{0:D2}:{1:D2}:{2:D2}", 
                    (int)RemainingTime.TotalHours, 
                    RemainingTime.Minutes, 
                    RemainingTime.Seconds);
            }
        }

        /// <summary>
        /// Gets the total seconds remaining
        /// </summary>
        public int TotalSecondsRemaining
        {
            get
            {
                return Math.Max(0, (int)RemainingTime.TotalSeconds);
            }
        }
    }
}