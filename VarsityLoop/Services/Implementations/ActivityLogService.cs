using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IFirestoreRepository<ActivityLog> _repository;

        public ActivityLogService(IFirestoreRepository<ActivityLog> repository)
        {
            _repository = repository;
        }

        public Task LogAsync(string actorId, string actorName, string action, string targetType, string targetId, string? details = null)
        {
            return _repository.AddAsync(new ActivityLog
            {
                ActorId = actorId,
                ActorName = actorName,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Details = details
            });
        }

        public async Task<List<ActivityLog>> GetRecentAsync(int count = 200)
        {
            var all = await _repository.GetAllAsync();
            return all.OrderByDescending(l => l.CreatedAt).Take(count).ToList();
        }
    }
}
