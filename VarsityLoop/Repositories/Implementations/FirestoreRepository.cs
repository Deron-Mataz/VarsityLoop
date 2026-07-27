using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;
using VarsityLoop.Repositories.Interfaces;

namespace VarsityLoop.Repositories.Implementations
{
    /// <summary>
    /// Generic Firestore CRUD implementation. One instance is registered per
    /// entity type via DI (see ServiceCollectionExtensions), each bound to its
    /// own collection name - so adding a brand-new marketplace module later is
    /// just: new entity class + one DI registration line, no new repository code.
    /// </summary>
    public class FirestoreRepository<T> : IFirestoreRepository<T> where T : BaseEntity, new()
    {
        private readonly FirestoreDb _db;
        private readonly string _collectionName;

        public FirestoreRepository(FirestoreDb db, string collectionName)
        {
            _db = db;
            _collectionName = collectionName;
        }

        private CollectionReference Collection => _db.Collection(_collectionName);

        public async Task<T?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var snapshot = await Collection.Document(id).GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var entity = snapshot.ConvertTo<T>();
            return entity.IsDeleted ? null : entity;
        }

        public async Task<List<T>> GetAllAsync(bool includeDeleted = false)
        {
            var query = includeDeleted
                ? Collection
                : (Query)Collection.WhereEqualTo("isDeleted", false);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<T>()).ToList();
        }

        public async Task<PagedResult<T>> GetPagedAsync(int pageSize, string? pageToken = null)
        {
            Query query = Collection
                .WhereEqualTo("isDeleted", false)
                .OrderByDescending("createdAt")
                .Limit(pageSize + 1); // fetch one extra to know if there's a next page

            if (!string.IsNullOrEmpty(pageToken))
            {
                var cursorDoc = await Collection.Document(pageToken).GetSnapshotAsync();
                if (cursorDoc.Exists)
                {
                    query = query.StartAfter(cursorDoc);
                }
            }

            var snapshot = await query.GetSnapshotAsync();
            var docs = snapshot.Documents.ToList();

            var hasMore = docs.Count > pageSize;
            var pageDocs = hasMore ? docs.Take(pageSize).ToList() : docs;

            return new PagedResult<T>
            {
                Items = pageDocs.Select(d => d.ConvertTo<T>()).ToList(),
                HasMore = hasMore,
                PageSize = pageSize,
                NextPageToken = hasMore ? pageDocs.Last().Id : null
            };
        }

        public async Task<string> AddAsync(T entity)
        {
            entity.CreatedAt = Timestamp.GetCurrentTimestamp();
            entity.UpdatedAt = Timestamp.GetCurrentTimestamp();
            entity.IsDeleted = false;

            DocumentReference docRef;

            if (!string.IsNullOrWhiteSpace(entity.Id))
            {
                // Caller wants a specific document Id (e.g. Users collection keyed by Firebase UID)
                docRef = Collection.Document(entity.Id);
                await docRef.SetAsync(entity);
            }
            else
            {
                docRef = await Collection.AddAsync(entity);
            }

            return docRef.Id;
        }

        public async Task UpdateAsync(string id, T entity)
        {
            entity.UpdatedAt = Timestamp.GetCurrentTimestamp();
            await Collection.Document(id).SetAsync(entity, SetOptions.Overwrite);
        }

        public async Task UpdateFieldsAsync(string id, Dictionary<string, object?> fields)
        {
            fields["updatedAt"] = Timestamp.GetCurrentTimestamp();
            await Collection.Document(id).UpdateAsync(fields!);
        }

        public async Task SoftDeleteAsync(string id)
        {
            await Collection.Document(id).UpdateAsync(new Dictionary<string, object>
            {
                { "isDeleted", true },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            });
        }

        public async Task HardDeleteAsync(string id)
        {
            await Collection.Document(id).DeleteAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            var snapshot = await Collection.Document(id).GetSnapshotAsync();
            return snapshot.Exists;
        }
    }
}
