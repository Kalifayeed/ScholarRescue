namespace ScholarRescue.ViewModels.Communication
{
    /// <summary>
    /// View model for the communication hub index.
    /// </summary>
    public class CommunicationHubViewModel
    {
        public string ActiveTab { get; set; } = "messages";
        public int UnreadMessageCount { get; set; }
        public int UnreadNotificationCount { get; set; }
        public int UnreadAnnouncementCount { get; set; }
        public int OpenTicketCount { get; set; }
    }
}