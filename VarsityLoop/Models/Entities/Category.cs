using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Which field set / listing form a category maps to. Reserved values for
    /// Section 2 phases not yet built (Fashion, StudySupplies, Accommodation,
    /// Services) are included now so Category records created today don't
    /// need a schema change when those phases land - only new UI/behavior
    /// keyed off the same enum value.
    /// </summary>
    public enum CategoryModule
    {
        Books,
        Electronics,
        Fashion,
        StudySupplies,
        Accommodation,
        Services
    }

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

        /// <summary>
        /// Which field set the listing form shows for listings under this
        /// category - e.g. "Books" categories get Author/ISBN/Course fields,
        /// "Electronics" categories get Type/Brand/Model/Specifications. This
        /// is what lets one Listing entity and one form serve every module
        /// without duplicating forms per category, per the Section 2 spec.
        /// </summary>
        [FirestoreProperty("module")]
        public string Module { get; set; } = CategoryModule.Books.ToString();
    }
}
