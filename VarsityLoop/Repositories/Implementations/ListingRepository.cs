using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;
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

        public async Task<PagedResult<Listing>> GetActivePagedAsync(int pageSize, string? pageToken = null)
        {
            Query query = _db.Collection(CollectionName)
                .WhereEqualTo("isDeleted", false)
                .WhereEqualTo("status", ListingStatus.Active.ToString())
                .OrderByDescending("createdAt")
                .Limit(pageSize + 1);

            if (!string.IsNullOrEmpty(pageToken))
            {
                var cursorDoc = await _db.Collection(CollectionName).Document(pageToken).GetSnapshotAsync();
                if (cursorDoc.Exists)
                {
                    query = query.StartAfter(cursorDoc);
                }
            }

            var snapshot = await query.GetSnapshotAsync();
            var docs = snapshot.Documents.ToList();
            var hasMore = docs.Count > pageSize;
            var pageDocs = hasMore ? docs.Take(pageSize).ToList() : docs;

            return new PagedResult<Listing>
            {
                Items = pageDocs.Select(d => d.ConvertTo<Listing>()).ToList(),
                HasMore = hasMore,
                PageSize = pageSize,
                NextPageToken = hasMore ? pageDocs.Last().Id : null
            };
        }

        public async Task<List<Listing>> GetBySellerAsync(string sellerId)
        {
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("sellerId", sellerId)
                .WhereEqualTo("isDeleted", false)
                .OrderByDescending("createdAt");

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<Listing>()).ToList();
        }

        public async Task<List<Listing>> SearchAsync(string searchTerm)
        {
            // Firestore has no native full-text search. For MVP scale, pull active
            // listings and filter server-side in memory across the searchable
            // fields; if the catalogue grows large this should move to a
            // dedicated search index (e.g. Algolia/Typesense) - see Phase 5.
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("isDeleted", false)
                .WhereEqualTo("status", ListingStatus.Active.ToString());

            var snapshot = await query.GetSnapshotAsync();
            var all = snapshot.Documents.Select(d => d.ConvertTo<Listing>()).ToList();

            var term = searchTerm.Trim().ToLowerInvariant();

            return all.Where(l =>
                l.Title.ToLowerInvariant().Contains(term) ||
                (l.Author?.ToLowerInvariant().Contains(term) ?? false) ||
                (l.Isbn?.ToLowerInvariant().Contains(term) ?? false) ||
                (l.Course?.ToLowerInvariant().Contains(term) ?? false) ||
                (l.Faculty?.ToLowerInvariant().Contains(term) ?? false) ||
                l.University.ToLowerInvariant().Contains(term) ||
                l.SellerName.ToLowerInvariant().Contains(term)
            ).ToList();
        }

        public async Task IncrementViewsAsync(string listingId)
        {
            await _db.Collection(CollectionName).Document(listingId).UpdateAsync("views", FieldValue.Increment(1));
        }
    }
}
