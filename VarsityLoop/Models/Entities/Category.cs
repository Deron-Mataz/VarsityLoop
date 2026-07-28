using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Stored in the "Categories" collection. Deliberately minimal and not
    /// tied to books - Faculty/Course filtering for textbooks lives on the
    /// Listing itself (Phase 4), while Category here is the broad grouping
    /// shown in "Browse Categories" and used as a Listing filter.
    /// </summary>
    [FirestoreData]
    public class Category : BaseEntity
    {
        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string? Description { get; set; }

        [FirestoreProperty("displayOrder")]
        public int DisplayOrder { get; set; } = 0;
    }
}
