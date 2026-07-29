using Google.Cloud.Firestore;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;

namespace VarsityLoop.Repositories.Implementations
{
    public class FavoriteRepository : FirestoreRepository<Favorite>, IFavoriteRepository
    {
        private readonly FirestoreDb _db;
        private const string CollectionName = "Favorites";

        public FavoriteRepository(FirestoreDb db) : base(db, CollectionName)
        {
            _db = db;
        }

        public async Task<List<Favorite>> GetByUserAsync(string userId)
        {
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("userId", userId)
                .WhereEqualTo("isDeleted", false);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<Favorite>()).ToList();
        }
    }
}
