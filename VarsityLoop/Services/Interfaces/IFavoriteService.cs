using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<bool> IsFavoritedAsync(string userId, string listingId);
        Task ToggleAsync(string userId, string listingId);

        /// <summary>Resolves each favorite to its current Listing, skipping any
        /// that have since been deleted (favorite entries for those are cleaned
        /// up as they're found, rather than left dangling).</summary>
        Task<List<Listing>> GetUserFavoriteListingsAsync(string userId);
    }
}
