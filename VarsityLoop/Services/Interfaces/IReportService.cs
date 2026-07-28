using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    public interface IReportService
    {
        Task<OperationResult> CreateAsync(string listingId, string listingTitle, string reporterId, string reporterName, string reason);
        Task<List<Report>> GetAllAsync();
        Task<OperationResult> ResolveAsync(string id, string actorId, string actorName);
        Task<OperationResult> DismissAsync(string id, string actorId, string actorName);
    }
}
