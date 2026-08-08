using Google.Cloud.Firestore;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;

namespace VarsityLoop.Repositories.Implementations
{
    public class AccommodationRepository : FirestoreRepository<Accommodation>, IAccommodationRepository
    {
        private readonly FirestoreDb _db;
        private const string CollectionName = "Accommodations";

        public AccommodationRepository(FirestoreDb db) : base(db, CollectionName)
        {
            _db = db;
        }

        public async Task<List<Accommodation>> GetAllActiveAsync()
        {
            // Two equality filters, no orderBy - no composite index needed
            // (same reasoning as ListingRepository.GetAllActiveAsync).
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("isDeleted", false)
                .WhereEqualTo("status", AccommodationStatus.Active.ToString());

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents
                .Select(d => d.ConvertTo<Accommodation>())
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
        }

        public async Task<List<Accommodation>> GetByLandlordAsync(string landlordId)
        {
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("landlordId", landlordId)
                .WhereEqualTo("isDeleted", false);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents
                .Select(d => d.ConvertTo<Accommodation>())
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
        }

        public async Task IncrementViewsAsync(string accommodationId)
        {
            await _db.Collection(CollectionName).Document(accommodationId).UpdateAsync("views", FieldValue.Increment(1));
        }
    }
}
