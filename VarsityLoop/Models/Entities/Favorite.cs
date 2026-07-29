using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    /// <summary>
    /// Stored in the "Favorites" collection. Document Id is deliberately
    /// "{userId}_{listingId}" (not auto-generated) so adding/removing a
    /// favorite is an idempotent set/delete on a known Id rather than a
    /// query-then-mutate - two rapid clicks can't create duplicates.
    /// </summary>
    [FirestoreData]
    public class Favorite : BaseEntity
    {
        public static string BuildId(string userId, string listingId) => $"{userId}_{listingId}";

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("listingId")]
        public string ListingId { get; set; } = string.Empty;
    }
}
