using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IListingService _listingService;

        public FavoriteService(IFavoriteRepository favoriteRepository, IListingService listingService)
        {
            _favoriteRepository = favoriteRepository;
            _listingService = listingService;
        }

        public async Task<bool> IsFavoritedAsync(string userId, string listingId)
        {
            var id = Favorite.BuildId(userId, listingId);
            return await _favoriteRepository.GetByIdAsync(id) != null;
        }

        public async Task ToggleAsync(string userId, string listingId)
        {
            var id = Favorite.BuildId(userId, listingId);
            var existing = await _favoriteRepository.GetByIdAsync(id);

            if (existing != null)
            {
                await _favoriteRepository.SoftDeleteAsync(id);
            }
            else
            {
                await _favoriteRepository.AddAsync(new Favorite
                {
                    Id = id,
                    UserId = userId,
                    ListingId = listingId
                });
            }
        }

        public async Task<List<Listing>> GetUserFavoriteListingsAsync(string userId)
        {
            var favorites = await _favoriteRepository.GetByUserAsync(userId);
            var listings = new List<Listing>();

            foreach (var favorite in favorites)
            {
                var listing = await _listingService.GetDetailsAsync(favorite.ListingId, countView: false);

                if (listing == null)
                {
                    // Listing was deleted since being favorited - clean up the
                    // dangling favorite rather than leaving it around forever.
                    await _favoriteRepository.HardDeleteAsync(favorite.Id);
                    continue;
                }

                listings.Add(listing);
            }

            return listings;
        }
    }
}
