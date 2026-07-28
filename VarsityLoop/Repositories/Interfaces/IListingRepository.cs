using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Repositories.Interfaces
{
    public interface IListingRepository : IFirestoreRepository<Listing>
    {
        Task<PagedResult<Listing>> GetActivePagedAsync(int pageSize, string? pageToken = null);
        Task<List<Listing>> GetBySellerAsync(string sellerId);
        Task<List<Listing>> SearchAsync(string searchTerm);
        Task IncrementViewsAsync(string listingId);
    }
}
