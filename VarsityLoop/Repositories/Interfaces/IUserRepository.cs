using VarsityLoop.Models.Entities;

namespace VarsityLoop.Repositories.Interfaces
{
    /// <summary>
    /// Adds user-specific lookups on top of the generic Firestore CRUD contract.
    /// </summary>
    public interface IUserRepository : IFirestoreRepository<ApplicationUser>
    {
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<ApplicationUser?> GetByFirebaseUidAsync(string firebaseUid);
        Task<List<ApplicationUser>> SearchAsync(string searchTerm);
    }
}
