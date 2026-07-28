using Google.Cloud.Firestore;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;

namespace VarsityLoop.Repositories.Implementations
{
    public class ListingRepository : FirestoreRepository<Listing>, IListingRepository
    {
        private readonly FirestoreDb _db;
        private const string CollectionName = "Listings";

        public ListingRepository(FirestoreDb db) : base(db, CollectionName)
        {
            _db = db;
        }

        public async Task<List<Listing>> GetAllActiveAsync()
        {
            // Two equality filters, no orderBy - Firestore covers this with its
            // automatic single-field indexes (no composite index needed).
            // Sorting is done by the caller/service layer instead.
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("isDeleted", false)
                .WhereEqualTo("status", ListingStatus.Active.ToString());

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents
                .Select(d => d.ConvertTo<Listing>())
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        public async Task<List<Listing>> GetBySellerAsync(string sellerId)
        {
            // Same reasoning as GetAllActiveAsync - equality filters only, sort
            // happens after the fetch so no composite index is required.
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("sellerId", sellerId)
                .WhereEqualTo("isDeleted", false);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents
                .Select(d => d.ConvertTo<Listing>())
                .OrderByDescending(l => l.CreatedAt)
                .ToList();
        }

        public async Task IncrementViewsAsync(string listingId)
        {
            await _db.Collection(CollectionName).Document(listingId).UpdateAsync("views", FieldValue.Increment(1));
        }
    }
}
