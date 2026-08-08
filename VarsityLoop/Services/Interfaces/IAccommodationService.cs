using VarsityLoop.Models.Common;
using VarsityLoop.Models.ViewModels.Accommodation;

namespace VarsityLoop.Services.Interfaces
{
    public interface IAccommodationService
    {
        Task<AccommodationBrowseResult> BrowseAsync(AccommodationBrowseQuery query);
        Task<Models.Entities.Accommodation?> GetDetailsAsync(string id, bool countView);
        Task<List<Models.Entities.Accommodation>> GetMyResidencesAsync(string landlordId);

        Task<OperationResult<string>> CreateAsync(AccommodationFormViewModel model, string landlordId, string landlordName, bool isVerifiedLandlord);
        Task<OperationResult> UpdateAsync(AccommodationFormViewModel model, string currentUserId, bool currentUserIsModerator);
        Task<OperationResult> DeleteAsync(string id, string currentUserId, bool currentUserIsModerator);
        Task<OperationResult> SetPausedAsync(string id, bool paused, string currentUserId, bool currentUserIsModerator);
    }
}
