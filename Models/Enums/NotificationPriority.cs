namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Priority levels for notifications with color-coded badge display.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>Low priority - informational only.</summary>
        Low = 0,

        /// <summary>Normal priority - standard notification.</summary>
        Normal = 1,

        /// <summary>High priority - requires attention.</summary>
        High = 2,

        /// <summary>Critical priority - immediate action required.</summary>
        Critical = 3
    }
}