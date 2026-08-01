using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// The fixed, system-defined Marketplace modules. NOT editable by admins -
    /// admins create Categories under a Module, they never create Modules
    /// themselves (per the "Marketplace > Module > Category > Listing"
    /// hierarchy). Accommodation and Services are deliberately excluded: they
    /// are separate platform areas (Accommodation gets its own module in
    /// Phase 11; Services is future work), not Marketplace categories.
    /// </summary>
    public enum CategoryModule
    {
        Books,
        Electronics,
        Fashion,
        Accessories,
        StudySupplies
    }

    /// <summary>
    /// Stored in the "Categories" collection. Categories organise listings
    /// within a Module - they are NOT product types (the seller specifies the
    /// actual product via the Listing's Type field). Faculty/Course filtering
    /// for textbooks lives on the Listing itself (Phase 4).
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
        /// "Electronics"/"StudySupplies" get Type/Brand/Model/Specifications,
        /// "Fashion"/"Accessories" get Type/Brand/Colour/Model/Size. This is
        /// what lets one Listing entity and one form serve every module
        /// without duplicating forms per category.
        /// </summary>
        [FirestoreProperty("module")]
        public string Module { get; set; } = CategoryModule.Books.ToString();

        /// <summary>
        /// Bootstrap Icons class name (e.g. "bi-book"), chosen manually by the
        /// admin from a module-appropriate list at creation time - never
        /// auto-assigned. Falls back to a generic icon in the UI if empty.
        /// </summary>
        [FirestoreProperty("iconClass")]
        public string? IconClass { get; set; }
    }
}
