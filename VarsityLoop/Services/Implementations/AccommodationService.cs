using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Accommodation;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class AccommodationService : IAccommodationService
    {
        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxGalleryImages = 10;

        private readonly IAccommodationRepository _repository;
        private readonly IStorageService _storageService;
        private readonly ILogger<AccommodationService> _logger;

        public AccommodationService(IAccommodationRepository repository, IStorageService storageService, ILogger<AccommodationService> logger)
        {
            _repository = repository;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<AccommodationBrowseResult> BrowseAsync(AccommodationBrowseQuery query)
        {
            var all = await _repository.GetAllActiveAsync();

            IEnumerable<Accommodation> filtered = all;

            if (!string.IsNullOrWhiteSpace(query.University))
            {
                var term = query.University.Trim().ToLowerInvariant();
                filtered = filtered.Where(a => a.University.ToLowerInvariant().Contains(term));
            }

            if (query.Classification.HasValue)
            {
                filtered = filtered.Where(a => a.Classification == query.Classification.Value.ToString());
            }

            if (query.AccommodationType.HasValue)
            {
                filtered = filtered.Where(a => a.AccommodationType == query.AccommodationType.Value.ToString());
            }

            if (query.MaxRent.HasValue)
            {
                filtered = filtered.Where(a => a.MonthlyRent <= query.MaxRent.Value);
            }

            if (query.GenderPreference.HasValue)
            {
                filtered = filtered.Where(a => a.GenderPreference == query.GenderPreference.Value.ToString() || a.GenderPreference == nameof(Models.Entities.GenderPreference.Any));
            }

            var filteredList = filtered.OrderByDescending(a => a.CreatedAt).ToList();
            var pageSize = query.PageSize <= 0 ? 12 : query.PageSize;
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageItems = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new AccommodationBrowseResult
            {
                Items = pageItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = filteredList.Count
            };
        }

        public async Task<Accommodation?> GetDetailsAsync(string id, bool countView)
        {
            var accommodation = await _repository.GetByIdAsync(id);

            if (accommodation != null && countView)
            {
                await _repository.IncrementViewsAsync(id);
                accommodation.Views += 1;
            }

            return accommodation;
        }

        public Task<List<Accommodation>> GetMyResidencesAsync(string landlordId)
            => _repository.GetByLandlordAsync(landlordId);

        public async Task<OperationResult<string>> CreateAsync(AccommodationFormViewModel model, string landlordId, string landlordName, bool isVerifiedLandlord)
        {
            // Enforced here, not just hidden in the UI - even a direct POST to
            // this action can't bypass the landlord verification gate.
            if (!isVerifiedLandlord)
            {
                return OperationResult<string>.Fail(
                    "Only verified landlords can publish accommodation listings. Complete landlord verification first.");
            }

            var galleryFiles = model.GalleryFiles?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();

            if (galleryFiles.Count == 0)
            {
                return OperationResult<string>.Fail("Please add at least one photo to the gallery.");
            }

            var validationError = ValidateImages(galleryFiles);
            if (validationError != null)
            {
                return OperationResult<string>.Fail(validationError);
            }

            var accommodationId = Guid.NewGuid().ToString("N");

            List<string> galleryUrls;
            try
            {
                galleryUrls = await UploadGalleryAsync(galleryFiles, accommodationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gallery upload failed while creating accommodation {AccommodationId} for landlord {LandlordId}", accommodationId, landlordId);
                return OperationResult<string>.Fail("We couldn't upload your photos, so the listing was NOT published. Please try again.");
            }

            var accommodation = new Accommodation
            {
                Id = accommodationId,
                ResidenceName = model.ResidenceName.Trim(),
                Classification = model.Classification.ToString(),
                AccommodationType = model.AccommodationType.ToString(),
                MonthlyRent = model.MonthlyRent,
                Deposit = model.Deposit,
                University = model.University.Trim(),
                DistanceFromCampus = model.DistanceFromCampus?.Trim(),
                AvailableFrom = Timestamp.FromDateTime(DateTime.SpecifyKind(model.AvailableFrom, DateTimeKind.Utc)),
                LeasePeriod = model.LeasePeriod?.Trim(),
                GenderPreference = model.GenderPreference.ToString(),
                Description = model.Description.Trim(),
                GoogleMapsUrl = model.GoogleMapsUrl?.Trim(),
                Gallery = galleryUrls,
                LandlordId = landlordId,
                LandlordName = landlordName,
                Status = AccommodationStatus.Active.ToString()
            };

            try
            {
                await _repository.AddAsync(accommodation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore write failed while creating accommodation {AccommodationId} for landlord {LandlordId}", accommodationId, landlordId);
                return OperationResult<string>.Fail("We uploaded your photos but couldn't save the listing itself, so it was NOT published. Please try again.");
            }

            return OperationResult<string>.Ok(accommodationId);
        }

        public async Task<OperationResult> UpdateAsync(AccommodationFormViewModel model, string currentUserId, bool currentUserIsModerator)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return OperationResult.Fail("Missing residence Id.");
            }

            var accommodation = await _repository.GetByIdAsync(model.Id);
            if (accommodation == null)
            {
                return OperationResult.Fail("Residence not found.");
            }

            if (accommodation.LandlordId != currentUserId && !currentUserIsModerator)
            {
                return OperationResult.Fail("You don't have permission to edit this residence.");
            }

            var newGalleryFiles = model.GalleryFiles?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
            var totalImageCount = model.ExistingGalleryUrls.Count + newGalleryFiles.Count;

            if (totalImageCount == 0)
            {
                return OperationResult.Fail("Please keep or add at least one gallery photo.");
            }

            if (totalImageCount > MaxGalleryImages)
            {
                return OperationResult.Fail($"A residence can have at most {MaxGalleryImages} gallery photos.");
            }

            if (newGalleryFiles.Count > 0)
            {
                var validationError = ValidateImages(newGalleryFiles);
                if (validationError != null)
                {
                    return OperationResult.Fail(validationError);
                }
            }

            List<string> newlyUploadedUrls;
            try
            {
                newlyUploadedUrls = newGalleryFiles.Count > 0
                    ? await UploadGalleryAsync(newGalleryFiles, accommodation.Id)
                    : new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gallery upload failed while updating accommodation {AccommodationId}", accommodation.Id);
                return OperationResult.Fail("We couldn't upload your new photos, so none of your changes were saved. Please try again.");
            }

            accommodation.ResidenceName = model.ResidenceName.Trim();
            accommodation.Classification = model.Classification.ToString();
            accommodation.AccommodationType = model.AccommodationType.ToString();
            accommodation.MonthlyRent = model.MonthlyRent;
            accommodation.Deposit = model.Deposit;
            accommodation.University = model.University.Trim();
            accommodation.DistanceFromCampus = model.DistanceFromCampus?.Trim();
            accommodation.AvailableFrom = Timestamp.FromDateTime(DateTime.SpecifyKind(model.AvailableFrom, DateTimeKind.Utc));
            accommodation.LeasePeriod = model.LeasePeriod?.Trim();
            accommodation.GenderPreference = model.GenderPreference.ToString();
            accommodation.Description = model.Description.Trim();
            accommodation.GoogleMapsUrl = model.GoogleMapsUrl?.Trim();
            accommodation.Gallery = model.ExistingGalleryUrls.Concat(newlyUploadedUrls).ToList();

            try
            {
                await _repository.UpdateAsync(accommodation.Id, accommodation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore write failed while updating accommodation {AccommodationId}", accommodation.Id);
                return OperationResult.Fail("We couldn't save your changes. Please try again.");
            }

            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string id, string currentUserId, bool currentUserIsModerator)
        {
            var accommodation = await _repository.GetByIdAsync(id);
            if (accommodation == null) return OperationResult.Fail("Residence not found.");

            if (accommodation.LandlordId != currentUserId && !currentUserIsModerator)
            {
                return OperationResult.Fail("You don't have permission to delete this residence.");
            }

            await _repository.SoftDeleteAsync(id);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> SetPausedAsync(string id, bool paused, string currentUserId, bool currentUserIsModerator)
        {
            var accommodation = await _repository.GetByIdAsync(id);
            if (accommodation == null) return OperationResult.Fail("Residence not found.");

            if (accommodation.LandlordId != currentUserId && !currentUserIsModerator)
            {
                return OperationResult.Fail("You don't have permission to change this residence.");
            }

            if (accommodation.Status != nameof(AccommodationStatus.Active) && accommodation.Status != nameof(AccommodationStatus.Paused))
            {
                return OperationResult.Fail("This residence can't be changed right now. Contact support.");
            }

            await _repository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", (paused ? AccommodationStatus.Paused : AccommodationStatus.Active).ToString() }
            });

            return OperationResult.Ok();
        }

        private async Task<List<string>> UploadGalleryAsync(List<IFormFile> files, string accommodationId)
        {
            var urls = new List<string>();

            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();
                var url = await _storageService.UploadPublicFileAsync(stream, file.FileName, file.ContentType, $"accommodation/{accommodationId}");
                urls.Add(url);
            }

            return urls;
        }

        private static string? ValidateImages(List<IFormFile> files)
        {
            if (files.Count > MaxGalleryImages)
            {
                return $"A residence can have at most {MaxGalleryImages} gallery photos.";
            }

            foreach (var file in files)
            {
                if (!AllowedImageTypes.Contains(file.ContentType))
                {
                    return "Photos must be JPG, PNG, or WEBP.";
                }

                if (file.Length > MaxImageBytes)
                {
                    return "Each photo must be under 5MB.";
                }
            }

            return null;
        }
    }
}
