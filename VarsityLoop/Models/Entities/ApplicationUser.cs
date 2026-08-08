using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Firestore document stored in the "Users" collection.
    /// Document Id matches the Firebase Authentication UID, so the two systems
    /// (Auth identity vs profile/role data) stay linked without duplicating a key.
    /// </summary>
    [FirestoreData]
    public class ApplicationUser : BaseEntity
    {
        [FirestoreProperty("firebaseUid")]
        public string FirebaseUid { get; set; } = string.Empty;

        [FirestoreProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [FirestoreProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("university")]
        public string University { get; set; } = string.Empty;

        [FirestoreProperty("profilePictureUrl")]
        public string? ProfilePictureUrl { get; set; }

        [FirestoreProperty("biography")]
        public string? Biography { get; set; }

        /// <summary>
        /// Stored as its string name (e.g. "User", "Admin") rather than the raw int,
        /// so the Firestore console stays human-readable and role changes made
        /// directly in the Admin Panel are unambiguous.
        /// </summary>
        [FirestoreProperty("role")]
        public string Role { get; set; } = RoleNames.User;

        [FirestoreProperty("accountStatus")]
        public string AccountStatus { get; set; } = Entities.AccountStatus.Active.ToString();

        [FirestoreProperty("emailVerified")]
        public bool EmailVerified { get; set; } = false;

        /// <summary>
        /// "None" (default - not a landlord), "Pending", "UnderReview", "Approved",
        /// "Rejected", "Suspended" (Phase 12 owns the full application/document
        /// workflow that drives these transitions). Only "Approved" may publish
        /// Accommodation listings - enforced in AccommodationService, not just
        /// the UI. For now, an Admin can set this manually from Admin > Users
        /// as a stopgap until the full verification flow exists.
        /// </summary>
        [FirestoreProperty("landlordVerificationStatus")]
        public string LandlordVerificationStatus { get; set; } = nameof(Entities.LandlordVerificationStatus.None);

        [FirestoreProperty("lastLoginAt")]
        public Timestamp? LastLoginAt { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
