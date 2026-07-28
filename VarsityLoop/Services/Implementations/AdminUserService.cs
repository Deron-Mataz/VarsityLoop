using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class AdminUserService : IAdminUserService
    {
        private static readonly string[] ValidRoles =
        {
            RoleNames.User, RoleNames.Moderator, RoleNames.Admin, RoleNames.SuperAdmin
        };

        private readonly IUserRepository _userRepository;
        private readonly IActivityLogService _activityLogService;

        public AdminUserService(IUserRepository userRepository, IActivityLogService activityLogService)
        {
            _userRepository = userRepository;
            _activityLogService = activityLogService;
        }

        public Task<List<ApplicationUser>> GetAllAsync() => _userRepository.GetAllAsync();

        public Task<List<ApplicationUser>> SearchAsync(string term) => _userRepository.SearchAsync(term);

        public async Task<OperationResult> SetRoleAsync(string userId, string newRole, string actorId, string actorName, bool actorIsSuperAdmin)
        {
            if (!ValidRoles.Contains(newRole))
            {
                return OperationResult.Fail("Not a valid role.");
            }

            if (userId == actorId)
            {
                return OperationResult.Fail("You can't change your own role.");
            }

            // Only a SuperAdmin can grant or revoke the SuperAdmin role - an Admin
            // promoting someone to SuperAdmin (or demoting an existing one) would
            // be a privilege escalation an Admin shouldn't be able to trigger.
            var target = await _userRepository.GetByIdAsync(userId);
            if (target == null) return OperationResult.Fail("User not found.");

            var involvesSuperAdmin = newRole == RoleNames.SuperAdmin || target.Role == RoleNames.SuperAdmin;
            if (involvesSuperAdmin && !actorIsSuperAdmin)
            {
                return OperationResult.Fail("Only a Super Admin can grant or change the Super Admin role.");
            }

            var previousRole = target.Role;

            await _userRepository.UpdateFieldsAsync(userId, new Dictionary<string, object?>
            {
                { "role", newRole }
            });

            await _activityLogService.LogAsync(actorId, actorName, "Changed role", "User", userId, $"{previousRole} -> {newRole} ({target.Email})");
            return OperationResult.Ok();
        }

        public async Task<OperationResult> SetAccountStatusAsync(string userId, AccountStatus status, string actorId, string actorName)
        {
            if (userId == actorId)
            {
                return OperationResult.Fail("You can't change your own account status.");
            }

            var target = await _userRepository.GetByIdAsync(userId);
            if (target == null) return OperationResult.Fail("User not found.");

            await _userRepository.UpdateFieldsAsync(userId, new Dictionary<string, object?>
            {
                { "accountStatus", status.ToString() }
            });

            await _activityLogService.LogAsync(actorId, actorName, $"Set account status to {status}", "User", userId, target.Email);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string userId, string actorId, string actorName)
        {
            if (userId == actorId)
            {
                return OperationResult.Fail("You can't delete your own account.");
            }

            var target = await _userRepository.GetByIdAsync(userId);
            if (target == null) return OperationResult.Fail("User not found.");

            await _userRepository.SoftDeleteAsync(userId);
            await _activityLogService.LogAsync(actorId, actorName, "Deleted account", "User", userId, target.Email);
            return OperationResult.Ok();
        }
    }
}
