using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Listings;

namespace VarsityLoop.Services.Interfaces
{
    public interface IListingService
    {
        Task<ListingBrowseResult> BrowseAsync(ListingBrowseQuery query);
        Task<MarketplaceHomeFeed> GetHomeFeedAsync();
        Task<Listing?> GetDetailsAsync(string id, bool countView);
        Task<List<Listing>> GetMyListingsAsync(string sellerId);

        Task<OperationResult<string>> CreateAsync(ListingFormViewModel model, string sellerId, string sellerName);
        Task<OperationResult> UpdateAsync(ListingFormViewModel model, string currentUserId, bool currentUserIsModerator);
        Task<OperationResult> DeleteAsync(string id, string currentUserId, bool currentUserIsModerator);
        Task<OperationResult> SetPausedAsync(string id, bool paused, string currentUserId, bool currentUserIsModerator);

        // --- Admin moderation (Phase 6) ---
        Task<List<Listing>> GetAllForAdminAsync(string? searchTerm, string? status);
        Task<OperationResult> SuspendAsync(string id, string actorId, string actorName);
        Task<OperationResult> RestoreAsync(string id, string actorId, string actorName);
        Task<OperationResult> RemoveAsync(string id, string actorId, string actorName);
    }
}
