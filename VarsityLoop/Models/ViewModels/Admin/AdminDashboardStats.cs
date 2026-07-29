namespace VarsityLoop.Models.ViewModels.Admin
{
    public class AdminDashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int PausedListings { get; set; }
        public int SuspendedOrRemovedListings { get; set; }
        public int TotalCategories { get; set; }
        public int PendingReports { get; set; }
    }
}
