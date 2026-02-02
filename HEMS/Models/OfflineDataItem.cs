using System;

namespace HEMS.Models
{
    /// <summary>
    /// Represents an offline data item for network synchronization
    /// </summary>
    public class OfflineDataItem
    {
        /// <summary>
        /// Type of offline data item (answer, flag, etc.)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// ID of the question this item relates to
        /// </summary>
        public int QuestionId { get; set; }

        /// <summary>
        /// ID of the selected choice (for answer items)
        /// </summary>
        public int? ChoiceId { get; set; }

        /// <summary>
        /// Flag status (for flag items)
        /// </summary>
        public bool? IsFlagged { get; set; }

        /// <summary>
        /// Timestamp when the item was created offline
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Number of sync attempts for this item
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Whether this item has been successfully synced
        /// </summary>
        public bool Synced { get; set; }
    }
}