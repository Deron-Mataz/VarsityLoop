using Microsoft.AspNetCore.Http;
using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Listings;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class ListingService : IListingService
    {
        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
        private const int MaxImagesPerListing = 6;

        private readonly IListingRepository _listingRepository;
        private readonly IStorageService _storageService;
        private readonly ICategoryService _categoryService;
        private readonly IActivityLogService _activityLogService;

        public ListingService(
            IListingRepository listingRepository,
            IStorageService storageService,
            ICategoryService categoryService,
            IActivityLogService activityLogService)
        {
            _listingRepository = listingRepository;
            _storageService = storageService;
            _categoryService = categoryService;
            _activityLogService = activityLogService;
        }

        public async Task<ListingBrowseResult> BrowseAsync(ListingBrowseQuery query)
        {
            var all = await _listingRepository.GetAllActiveAsync();

            IEnumerable<Listing> filtered = all;

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim().ToLowerInvariant();
                filtered = filtered.Where(l =>
                    l.Title.ToLowerInvariant().Contains(term) ||
                    (l.Author?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Isbn?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Course?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Faculty?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Type?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Brand?.ToLowerInvariant().Contains(term) ?? false) ||
                    (l.Model?.ToLowerInvariant().Contains(term) ?? false) ||
                    l.University.ToLowerInvariant().Contains(term) ||
                    l.SellerName.ToLowerInvariant().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.CategoryId))
            {
                filtered = filtered.Where(l => l.CategoryId == query.CategoryId);
            }

            if (query.MinPrice.HasValue)
            {
                filtered = filtered.Where(l => l.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                filtered = filtered.Where(l => l.Price <= query.MaxPrice.Value);
            }

            if (query.Condition.HasValue)
            {
                filtered = filtered.Where(l => l.Condition == query.Condition.Value.ToString());
            }

            filtered = query.Sort switch
            {
                ListingSortOption.Oldest => filtered.OrderBy(l => l.CreatedAt),
                ListingSortOption.PriceLowToHigh => filtered.OrderBy(l => l.Price),
                ListingSortOption.PriceHighToLow => filtered.OrderByDescending(l => l.Price),
                _ => filtered.OrderByDescending(l => l.CreatedAt)
            };

            var filteredList = filtered.ToList();
            var pageSize = query.PageSize <= 0 ? 12 : query.PageSize;
            var page = query.Page <= 0 ? 1 : query.Page;

            var pageItems = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new ListingBrowseResult
            {
                Items = pageItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = filteredList.Count
            };
        }

        public async Task<Listing?> GetDetailsAsync(string id, bool countView)
        {
            var listing = await _listingRepository.GetByIdAsync(id);

            if (listing != null && countView)
            {
                await _listingRepository.IncrementViewsAsync(id);
                listing.Views += 1;
            }

            return listing;
        }

        public Task<List<Listing>> GetMyListingsAsync(string sellerId)
            => _listingRepository.GetBySellerAsync(sellerId);

        public async Task<OperationResult<string>> CreateAsync(ListingFormViewModel model, string sellerId, string sellerName)
        {
            var imageFiles = model.ImageFiles?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();

            if (imageFiles.Count == 0)
            {
                return OperationResult<string>.Fail("Please add at least one photo.");
            }

            var validationError = ValidateImages(imageFiles);
            if (validationError != null)
            {
                return OperationResult<string>.Fail(validationError);
            }

            var category = await _categoryService.GetByIdAsync(model.CategoryId);
            if (category == null)
            {
                return OperationResult<string>.Fail("Please choose a valid category.");
            }

            // Generate the document Id up front so uploaded images can live under
            // a folder named for the listing they belong to (listings/{id}/...).
            var listingId = Guid.NewGuid().ToString("N");
            var imageUrls = await UploadImagesAsync(imageFiles, listingId);

            var listing = new Listing
            {
                Id = listingId,
                CategoryId = category.Id,
                CategoryName = category.Name,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Price = model.Price,
                Author = model.Author?.Trim(),
                Isbn = model.Isbn?.Trim(),
                Course = model.Course?.Trim(),
                Faculty = model.Faculty?.Trim(),
                Condition = model.Condition.ToString(),
                University = model.University.Trim(),
                Location = model.Location?.Trim(),
                Type = model.Type?.Trim(),
                Brand = model.Brand?.Trim(),
                Model = model.Model?.Trim(),
                Specifications = model.Specifications?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>(),
                ImageUrls = imageUrls,
                SellerId = sellerId,
                SellerName = sellerName,
                Status = ListingStatus.Active.ToString()
            };

            await _listingRepository.AddAsync(listing);

            return OperationResult<string>.Ok(listingId);
        }

        public async Task<OperationResult> UpdateAsync(ListingFormViewModel model, string currentUserId, bool currentUserIsModerator)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return OperationResult.Fail("Missing listing Id.");
            }

            var listing = await _listingRepository.GetByIdAsync(model.Id);
            if (listing == null)
            {
                return OperationResult.Fail("Listing not found.");
            }

            if (listing.SellerId != currentUserId && !currentUserIsModerator)
            {
                return OperationResult.Fail("You don't have permission to edit this listing.");
            }

            var category = await _categoryService.GetByIdAsync(model.CategoryId);
            if (category == null)
            {
                return OperationResult.Fail("Please choose a valid category.");
            }

            var newImageFiles = model.ImageFiles?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
            var totalImageCount = model.ExistingImageUrls.Count + newImageFiles.Count;

            if (totalImageCount == 0)
            {
                return OperationResult.Fail("Please keep or add at least one photo.");
            }

            if (totalImageCount > MaxImagesPerListing)
            {
                return OperationResult.Fail($"A listing can have at most {MaxImagesPerListing} photos.");
            }

            if (newImageFiles.Count > 0)
            {
                var validationError = ValidateImages(newImageFiles);
                if (validationError != null)
                {
                    return OperationResult.Fail(validationError);
                }
            }

            var newlyUploadedUrls = newImageFiles.Count > 0
                ? await UploadImagesAsync(newImageFiles, listing.Id)
                : new List<string>();

            listing.CategoryId = category.Id;
            listing.CategoryName = category.Name;
            listing.Title = model.Title.Trim();
            listing.Description = model.Description.Trim();
            listing.Price = model.Price;
            listing.Author = model.Author?.Trim();
            listing.Isbn = model.Isbn?.Trim();
            listing.Course = model.Course?.Trim();
            listing.Faculty = model.Faculty?.Trim();
            listing.Condition = model.Condition.ToString();
            listing.University = model.University.Trim();
            listing.Location = model.Location?.Trim();
            listing.Type = model.Type?.Trim();
            listing.Brand = model.Brand?.Trim();
            listing.Model = model.Model?.Trim();
            listing.Specifications = model.Specifications?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>();
            listing.ImageUrls = model.ExistingImageUrls.Concat(newlyUploadedUrls).ToList();

            await _listingRepository.UpdateAsync(listing.Id, listing);

            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string id, string currentUserId, bool currentUserIsModerator)
        {
            var listing = await _listingRepository.GetByIdAsync(id);
            if (listing == null) return OperationResult.Fail("Listing not found.");

            if (listing.SellerId != currentUserId && !currentUserIsModerator)
            {
                return OperationResult.Fail("You don't have permission to delete this listing.");
            }

            await _listingRepository.SoftDeleteAsync(id);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> SetPausedAsync(string id, bool paused, string currentUserId, bool currentUserIsModerator)
        {
            var listing = await _listingRepository.GetByIdAsync(id);
            if (listing == null) return OperationResult.Fail("Listing not found.");

            if (listing.SellerId != currentUserId && !currentUserIsModerator)
            {
                return OperationResult.Fail("You don't have permission to change this listing.");
            }

            if (listing.Status != nameof(ListingStatus.Active) && listing.Status != nameof(ListingStatus.Paused))
            {
                return OperationResult.Fail("This listing can't be changed right now. Contact support.");
            }

            await _listingRepository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", (paused ? ListingStatus.Paused : ListingStatus.Active).ToString() }
            });

            return OperationResult.Ok();
        }

        public async Task<List<Listing>> GetAllForAdminAsync(string? searchTerm, string? status)
        {
            // Admins need to see every status (including Suspended/Removed), so this
            // goes through the base repository's GetAllAsync (non-deleted only) rather
            // than GetAllActiveAsync, which is scoped to Status == Active for the
            // public Browse page.
            var all = await _listingRepository.GetAllAsync();

            IEnumerable<Listing> filtered = all;

            if (!string.IsNullOrWhiteSpace(status))
            {
                filtered = filtered.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                filtered = filtered.Where(l =>
                    l.Title.ToLowerInvariant().Contains(term) ||
                    l.SellerName.ToLowerInvariant().Contains(term) ||
                    l.University.ToLowerInvariant().Contains(term));
            }

            return filtered.OrderByDescending(l => l.CreatedAt).ToList();
        }

        public async Task<OperationResult> SuspendAsync(string id, string actorId, string actorName)
        {
            var listing = await _listingRepository.GetByIdAsync(id);
            if (listing == null) return OperationResult.Fail("Listing not found.");

            await _listingRepository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", ListingStatus.Suspended.ToString() }
            });

            await _activityLogService.LogAsync(actorId, actorName, "Suspended listing", "Listing", id, listing.Title);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> RestoreAsync(string id, string actorId, string actorName)
        {
            var listing = await _listingRepository.GetByIdAsync(id);
            if (listing == null) return OperationResult.Fail("Listing not found.");

            await _listingRepository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", ListingStatus.Active.ToString() }
            });

            await _activityLogService.LogAsync(actorId, actorName, "Restored listing", "Listing", id, listing.Title);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> RemoveAsync(string id, string actorId, string actorName)
        {
            var listing = await _listingRepository.GetByIdAsync(id);
            if (listing == null) return OperationResult.Fail("Listing not found.");

            await _listingRepository.UpdateFieldsAsync(id, new Dictionary<string, object?>
            {
                { "status", ListingStatus.Removed.ToString() }
            });

            await _activityLogService.LogAsync(actorId, actorName, "Removed listing", "Listing", id, listing.Title);
            return OperationResult.Ok();
        }

        private async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string listingId)
        {
            var urls = new List<string>();

            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();
                var url = await _storageService.UploadPublicFileAsync(stream, file.FileName, file.ContentType, $"listings/{listingId}");
                urls.Add(url);
            }

            return urls;
        }

        private static string? ValidateImages(List<IFormFile> files)
        {
            if (files.Count > MaxImagesPerListing)
            {
                return $"A listing can have at most {MaxImagesPerListing} photos.";
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
