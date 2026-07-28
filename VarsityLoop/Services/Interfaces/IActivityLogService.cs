using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(string actorId, string actorName, string action, string targetType, string targetId, string? details = null);
        Task<List<ActivityLog>> GetRecentAsync(int count = 200);
    }
}
