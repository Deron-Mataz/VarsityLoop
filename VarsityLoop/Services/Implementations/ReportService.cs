using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IFirestoreRepository<Report> _repository;
        private readonly IActivityLogService _activityLogService;

        public ReportService(IFirestoreRepository<Report> repository, IActivityLogService activityLogService)
        {
            _repository = repository;
            _activityLogService = activityLogService;
        }

        public async Task<OperationResult> CreateAsync(string listingId, string listingTitle, string reporterId, string reporterName, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return OperationResult.Fail("Please describe why you're reporting this listing.");
            }

            await _repository.AddAsync(new Report
            {
                ListingId = listingId,
                ListingTitle = listingTitle,
                ReporterId = reporterId,
                ReporterName = reporterName,
                Reason = reason.Trim(),
                Status = ReportStatus.Pending.ToString()
            });

            return OperationResult.Ok();
        }

        public async Task<List<Report>> GetAllAsync()
        {
            var all = await _repository.GetAllAsync();
            // Pending reports need attention first, then newest first within each group.
            return all
                .OrderBy(r => r.Status == nameof(ReportStatus.Pending) ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();
        }

        public async Task<OperationResult> ResolveAsync(string id, string actorId, string actorName)
        {
            var report = await _repository.GetByIdAsync(id);
            if (report == null) return OperationResult.Fail("Report not found.");

            await _repository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", ReportStatus.Resolved.ToString() }
            });

            await _activityLogService.LogAsync(actorId, actorName, "Resolved report", "Report", id, $"Listing: {report.ListingTitle}");
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DismissAsync(string id, string actorId, string actorName)
        {
            var report = await _repository.GetByIdAsync(id);
            if (report == null) return OperationResult.Fail("Report not found.");

            await _repository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", ReportStatus.Dismissed.ToString() }
            });

            await _activityLogService.LogAsync(actorId, actorName, "Dismissed report", "Report", id, $"Listing: {report.ListingTitle}");
            return OperationResult.Ok();
        }
    }
}
