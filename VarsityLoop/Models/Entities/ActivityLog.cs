using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Stored in the "ActivityLogs" collection. Written by AdminUserService,
    /// ListingService's admin-facing methods, and ReportService whenever a
    /// moderation action happens - never edited or deleted, purely an audit
    /// trail. CreatedAt (from BaseEntity) is the log timestamp.
    /// </summary>
    [FirestoreData]
    public class ActivityLog : BaseEntity
    {
        [FirestoreProperty("actorId")]
        public string ActorId { get; set; } = string.Empty;

        [FirestoreProperty("actorName")]
        public string ActorName { get; set; } = string.Empty;

        [FirestoreProperty("action")]
        public string Action { get; set; } = string.Empty;

        [FirestoreProperty("targetType")]
        public string TargetType { get; set; } = string.Empty;

        [FirestoreProperty("targetId")]
        public string TargetId { get; set; } = string.Empty;

        [FirestoreProperty("details")]
        public string? Details { get; set; }
    }
}
