using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Models
{
    /// <summary>
    /// Represents an application user with extended profile, capacity, quality, and audit fields.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }

        // ---- Writer Capacity & Availability ----
        public WriterAvailabilityStatus AvailabilityStatus { get; set; } = WriterAvailabilityStatus.Available;

        /// <summary>Maximum concurrent active orders (default: 5).</summary>
        public int MaxActiveOrders { get; set; } = 5;

        /// <summary>Current number of active/in-progress orders.</summary>
        public int CurrentActiveOrders { get; set; } = 0;

        /// <summary>Whether the writer is currently accepting new orders.</summary>
        public bool IsAcceptingOrders { get; set; } = true;

        /// <summary>Timestamp of the last completed order.</summary>
        public DateTime? LastOrderCompletedAt { get; set; }

        // ---- Writer Quality Metrics ----
        /// <summary>Quality score (0-100). Initial: 80.</summary>
        public int QualityScore { get; set; } = 80;

        /// <summary>Average client rating (1-5).</summary>
        public double AverageRating { get; set; }

        /// <summary>Total number of completed orders.</summary>
        public int TotalCompletedOrders { get; set; }

        /// <summary>Total number of revision requests received.</summary>
        public int TotalRevisionRequests { get; set; }

        /// <summary>Total number of late deliveries.</summary>
        public int TotalLateOrders { get; set; }

        /// <summary>Writer's subject specializations (comma-separated, up to 5).</summary>
        [MaxLength(500)]
        public string? SubjectSpecializations { get; set; }

        /// <summary>Writer's highest academic qualification.</summary>
        [MaxLength(150)]
        public string? Qualification { get; set; }

        /// <summary>Timestamp of the last activity.</summary>
        public DateTime? LastActivityDate { get; set; }

        // ---- Email Verification Fields ----
        /// <summary>When the user's email was verified (null if unverified).</summary>
        public DateTime? EmailVerifiedAt { get; set; }

        /// <summary>Whether the registration/setup process was completed.</summary>
        public bool RegistrationCompleted { get; set; } = true;

        /// <summary>How the user registered: "Direct", "OrderFlow", "WriterApplication".</summary>
        [MaxLength(50)]
        public string? RegistrationSource { get; set; }

        // ---- Writer ID & Screen Name (Phase 12C) ----
        /// <summary>Permanent Writer ID (e.g., SRW-10458). Unique across all writers.</summary>
        [MaxLength(20)]
        public string? WriterId { get; set; }

        /// <summary>Public screen name shown to clients instead of real name.</summary>
        [MaxLength(30)]
        public string? ScreenName { get; set; }

        /// <summary>Last time the screen name was changed.</summary>
        public DateTime? LastScreenNameChangeDate { get; set; }

        /// <summary>How many times the screen name has been changed.</summary>
        public int ScreenNameChangeCount { get; set; }

        // ---- IP & Device Tracking (Phase 12C) ----
        /// <summary>Last known IP address used by the user.</summary>
        [MaxLength(45)]
        public string? LastKnownIPAddress { get; set; }

        /// <summary>IP address used during registration.</summary>
        [MaxLength(45)]
        public string? RegistrationIPAddress { get; set; }

        // ---- Navigation Properties ----
        public virtual ICollection<Order> OrdersAsClient { get; set; } = new List<Order>();
        public virtual ICollection<Order> OrdersAsWriter { get; set; } = new List<Order>();
    }
}