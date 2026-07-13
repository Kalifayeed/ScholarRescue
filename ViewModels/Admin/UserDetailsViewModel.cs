using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using OrderEntity = ScholarRescue.Models.TutoringRequest;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for displaying detailed user information in admin panel.
    /// </summary>
    public class UserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<OrderEntity> OrdersAsClient { get; set; } = new();
        public List<OrderEntity> OrdersAsWriter { get; set; } = new();
        public WriterApplication? WriterApplication { get; set; }
    }
}
