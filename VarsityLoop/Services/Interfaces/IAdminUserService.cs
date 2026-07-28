using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    public interface IAdminUserService
    {
        Task<List<ApplicationUser>> GetAllAsync();
        Task<List<ApplicationUser>> SearchAsync(string term);

        Task<OperationResult> SetRoleAsync(string userId, string newRole, string actorId, string actorName, bool actorIsSuperAdmin);
        Task<OperationResult> SetAccountStatusAsync(string userId, AccountStatus status, string actorId, string actorName);
        Task<OperationResult> DeleteAsync(string userId, string actorId, string actorName);
    }
}
