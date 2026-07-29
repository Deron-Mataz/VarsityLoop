using VarsityLoop.Models.Entities;

namespace VarsityLoop.Repositories.Interfaces
{
    public interface IFavoriteRepository : IFirestoreRepository<Favorite>
    {
        Task<List<Favorite>> GetByUserAsync(string userId);
    }
}
