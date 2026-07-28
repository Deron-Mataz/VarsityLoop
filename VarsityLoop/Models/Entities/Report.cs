using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    public enum ReportStatus
    {
        Pending,
        Resolved,
        Dismissed
    }

    /// <summary>
    /// Stored in the "Reports" collection. ListingTitle/ReporterName are
    /// denormalized snapshots at the time of reporting, so the report still
    /// reads sensibly even if the listing or reporter's name changes later.
    /// </summary>
    [FirestoreData]
    public class Report : BaseEntity
    {
        [FirestoreProperty("listingId")]
        public string ListingId { get; set; } = string.Empty;

        [FirestoreProperty("listingTitle")]
        public string ListingTitle { get; set; } = string.Empty;

        [FirestoreProperty("reporterId")]
        public string ReporterId { get; set; } = string.Empty;

        [FirestoreProperty("reporterName")]
        public string ReporterName { get; set; } = string.Empty;

        [FirestoreProperty("reason")]
        public string Reason { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = ReportStatus.Pending.ToString();
    }
}
