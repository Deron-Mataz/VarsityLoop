using VarsityLoop.Models.Entities;

namespace VarsityLoop.Repositories.Interfaces
{
    public interface IAccommodationRepository : IFirestoreRepository<Accommodation>
    {
        Task<List<Accommodation>> GetAllActiveAsync();
        Task<List<Accommodation>> GetByLandlordAsync(string landlordId);
        Task IncrementViewsAsync(string accommodationId);
    }
}
