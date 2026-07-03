using ScholarRescue.ViewModels.Admin;

namespace ScholarRescue.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardViewModel> GetDashboardViewModelAsync();
    }
}
