using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for the writer applications list page.
    /// </summary>
    public class WriterApplicationsViewModel
    {
        public List<WriterApplication> Applications { get; set; } = new();
        public WriterApplicationStatus? CurrentFilter { get; set; }
    }
}