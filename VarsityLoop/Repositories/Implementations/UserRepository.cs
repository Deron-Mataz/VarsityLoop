using Google.Cloud.Firestore;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;

namespace VarsityLoop.Repositories.Implementations
{
    public class UserRepository : FirestoreRepository<ApplicationUser>, IUserRepository
    {
        private readonly FirestoreDb _db;
        private const string CollectionName = "Users";

        public UserRepository(FirestoreDb db) : base(db, CollectionName)
        {
            _db = db;
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            var query = _db.Collection(CollectionName)
                .WhereEqualTo("email", email.Trim().ToLowerInvariant())
                .WhereEqualTo("isDeleted", false)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Count > 0
                ? snapshot.Documents[0].ConvertTo<ApplicationUser>()
                : null;
        }

        public async Task<ApplicationUser?> GetByFirebaseUidAsync(string firebaseUid)
        {
            // Users collection is keyed by Firebase UID as the document Id,
            // so this is just a direct lookup via the base repository.
            return await GetByIdAsync(firebaseUid);
        }

        public async Task<List<ApplicationUser>> SearchAsync(string searchTerm)
        {
            // Firestore has no native full-text search. For MVP scale, pull active
            // users and filter server-side in memory; if the user base grows large
            // this should move to a dedicated search index (e.g. Algolia/Typesense).
            var all = await GetAllAsync();
            var term = searchTerm.Trim().ToLowerInvariant();

            return all.Where(u =>
                u.FullName.ToLowerInvariant().Contains(term) ||
                u.Email.ToLowerInvariant().Contains(term) ||
                u.University.ToLowerInvariant().Contains(term)
            ).ToList();
        }
    }
}
