using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Listings;

namespace VarsityLoop.Services.Interfaces
{
    public interface IListingService
    {
        Task<ListingBrowseResult> BrowseAsync(ListingBrowseQuery query);
        Task<Listing?> GetDetailsAsync(string id, bool countView);
        Task<List<Listing>> GetMyListingsAsync(string sellerId);

        Task<OperationResult<string>> CreateAsync(ListingFormViewModel model, string sellerId, string sellerName);
        Task<OperationResult> UpdateAsync(ListingFormViewModel model, string currentUserId, bool currentUserIsModerator);
        Task<OperationResult> DeleteAsync(string id, string currentUserId, bool currentUserIsModerator);
        Task<OperationResult> SetPausedAsync(string id, bool paused, string currentUserId, bool currentUserIsModerator);
    }
}
