using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    public enum ListingCondition
    {
        New,
        LikeNew,
        Good,
        Fair,
        Poor
    }

    public enum ListingStatus
    {
        Active,
        Paused,

        // Reserved for Admin moderation (Phase 6) - not settable by sellers themselves.
        Suspended,
        Removed
    }

    /// <summary>
    /// Stored in the "Listings" Firestore collection. Named generically (not
    /// "Book") because it's the shared shape every future marketplace module
    /// (Accommodation, Electronics, Services...) is meant to reuse - Category
    /// distinguishes them, not a different entity per module. The MVP only
    /// ever writes Category = "Textbooks", but nothing here assumes that.
    /// </summary>
    [FirestoreData]
    public class Listing : BaseEntity
    {
        [FirestoreProperty("categoryId")]
        public string CategoryId { get; set; } = string.Empty;

        [FirestoreProperty("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("price")]
        public double Price { get; set; }

        // --- Book-specific fields (MVP) ---
        [FirestoreProperty("author")]
        public string? Author { get; set; }

        [FirestoreProperty("isbn")]
        public string? Isbn { get; set; }

        [FirestoreProperty("course")]
        public string? Course { get; set; }

        [FirestoreProperty("faculty")]
        public string? Faculty { get; set; }

        [FirestoreProperty("condition")]
        public string Condition { get; set; } = ListingCondition.Good.ToString();

        // --- Electronics / Fashion / Study Supplies fields (Section 2) ---
        // Free-text rather than enums: the spec explicitly lists these as
        // "not limited to" suggestions, and locking them to a hardcoded enum
        // would mean a code change every time a seller needs a type/spec that
        // wasn't anticipated. The listing form still offers the suggested
        // values via an HTML datalist for a fast pick, without hardcoding them.
        [FirestoreProperty("type")]
        public string? Type { get; set; }

        [FirestoreProperty("brand")]
        public string? Brand { get; set; }

        [FirestoreProperty("model")]
        public string? Model { get; set; }

        [FirestoreProperty("specifications")]
        public List<string> Specifications { get; set; } = new();

        // --- Common to every listing, regardless of module ---
        [FirestoreProperty("university")]
        public string University { get; set; } = string.Empty;

        [FirestoreProperty("location")]
        public string? Location { get; set; }

        [FirestoreProperty("imageUrls")]
        public List<string> ImageUrls { get; set; } = new();

        [FirestoreProperty("sellerId")]
        public string SellerId { get; set; } = string.Empty;

        [FirestoreProperty("sellerName")]
        public string SellerName { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = ListingStatus.Active.ToString();

        [FirestoreProperty("views")]
        public int Views { get; set; } = 0;
    }
}
