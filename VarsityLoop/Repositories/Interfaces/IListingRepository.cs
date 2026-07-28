using VarsityLoop.Models.Entities;

namespace VarsityLoop.Repositories.Interfaces
{
    public interface IListingRepository : IFirestoreRepository<Listing>
    {
        /// <summary>
        /// All active, non-deleted listings, newest first. Uses two equality
        /// filters only (isDeleted, status) with no Firestore-side orderBy,
        /// so it needs no composite index - sorting happens here in memory
        /// instead. Fine at MVP catalogue size; if the catalogue grows large
        /// enough for this to matter, move filtering/sorting back to Firestore
        /// query clauses (or a dedicated search index) and pre-create the
        /// composite indexes it would need.
        /// </summary>
        Task<List<Listing>> GetAllActiveAsync();

        Task<List<Listing>> GetBySellerAsync(string sellerId);

        Task IncrementViewsAsync(string listingId);
    }
}
