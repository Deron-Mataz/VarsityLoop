using Google.Cloud.Firestore;

namespace VarsityLoop.Models.Common
{
    /// <summary>
    /// Every Firestore document model should inherit this so the generic
    /// repository layer can read/write the document Id and audit timestamps
    /// consistently, regardless of which collection it belongs to.
    /// </summary>
    [FirestoreData]
    public abstract class BaseEntity
    {
        /// <summary>
        /// Firestore document ID. Not stored as a field inside the document itself -
        /// the generic repository populates this from snapshot.Id after reads.
        /// </summary>
        [FirestoreDocumentId]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();

        [FirestoreProperty("updatedAt")]
        public Timestamp UpdatedAt { get; set; } = Timestamp.GetCurrentTimestamp();

        [FirestoreProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;
    }
}
