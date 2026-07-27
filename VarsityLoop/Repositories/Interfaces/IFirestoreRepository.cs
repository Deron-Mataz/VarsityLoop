using VarsityLoop.Models.Common;

namespace VarsityLoop.Repositories.Interfaces
{
    /// <summary>
    /// Generic CRUD contract implemented against a single Firestore collection.
    /// Keeping this generic (rather than one repository per entity with duplicated
    /// boilerplate) is what lets future modules - Accommodation, Electronics,
    /// Services - reuse the same data-access layer with zero new plumbing.
    /// </summary>
    public interface IFirestoreRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(string id);

        Task<List<T>> GetAllAsync(bool includeDeleted = false);

        Task<PagedResult<T>> GetPagedAsync(int pageSize, string? pageToken = null);

        Task<string> AddAsync(T entity);

        Task UpdateAsync(string id, T entity);

        /// <summary>Partial update - only the supplied fields are written.</summary>
        Task UpdateFieldsAsync(string id, Dictionary<string, object?> fields);

        /// <summary>Soft delete - sets IsDeleted = true rather than removing the document.</summary>
        Task SoftDeleteAsync(string id);

        /// <summary>Permanently removes the document. Use sparingly (e.g. GDPR requests).</summary>
        Task HardDeleteAsync(string id);

        Task<bool> ExistsAsync(string id);
    }
}
