namespace ScholarRescue.Models.Enums
{
    /// <summary>
    /// Defines the target audience for system announcements and broadcasts.
    /// </summary>
    public enum TargetAudience
    {
        /// <summary>All platform users.</summary>
        AllUsers = 0,

        /// <summary>Only clients (students).</summary>
        Clients = 1,

        /// <summary>Only writers (tutors).</summary>
        Writers = 2,

        /// <summary>Only administrators.</summary>
        Admins = 3
    }
}