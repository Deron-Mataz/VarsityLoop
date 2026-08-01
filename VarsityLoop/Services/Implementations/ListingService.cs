using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ListingService> _logger;

        public ListingService(
            IListingRepository listingRepository,
            IStorageService storageService,
            ICategoryService categoryService,
            IActivityLogService activityLogService,
            ILogger<ListingService> logger)
        {
            _listingRepository = listingRepository;
            _storageService = storageService;
            _categoryService = categoryService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        public async Task<MarketplaceHomeFeed> GetHomeFeedAsync()
        {
            // One read, bucketed in memory into every section - avoids six
            // separate Firestore round-trips for what's ultimately the same
            // underlying "all active listings" data.
            var all = await _listingRepository.GetAllActiveAsync(); // already newest-first

            return new MarketplaceHomeFeed
            {
                Featured = all.OrderByDescending(l => l.Views).Take(4).ToList(),
                RecentBooks = all.Where(l => l.Module == nameof(CategoryModule.Books)).Take(4).ToList(),
                TrendingElectronics = all.Where(l => l.Module == nameof(CategoryModule.Electronics)).OrderByDescending(l => l.Views).Take(4).ToList(),
                LatestFashion = all.Where(l => l.Module == nameof(CategoryModule.Fashion)).Take(4).ToList(),
                PopularAccessories = all.Where(l => l.Module == nameof(CategoryModule.Accessories)).OrderByDescending(l => l.Views).Take(4).ToList(),
                StudySupplies = all.Where(l => l.Module == nameof(CategoryModule.StudySupplies)).Take(4).ToList()
            };
        }

        public async Task<ListingBrowseResult> BrowseAsync(ListingBrowseQuery query)
        {
            var all = await _listingRepository.GetAllActiveAsync();

            IEnumerable<Listing> filtered = all;

            if (!string.IsNullOrWhiteSpace(query.Module))
            {
                filtered = filtered.Where(l => l.Module == query.Module);
            }

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
                    (l.Colour?.ToLowerInvariant().Contains(term) ?? false) ||
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

            Category? category;
            try
            {
                category = await _categoryService.GetByIdAsync(model.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to look up category {CategoryId} while creating a listing for seller {SellerId}", model.CategoryId, sellerId);
                return OperationResult<string>.Fail("We couldn't verify the selected category. Please try again in a moment.");
            }

            if (category == null)
            {
                return OperationResult<string>.Fail("Please choose a valid category.");
            }

            // Generate the document Id up front so uploaded images can live under
            // a folder named for the listing they belong to (listings/{id}/...).
            var listingId = Guid.NewGuid().ToString("N");

            List<string> imageUrls;
            try
            {
                imageUrls = await UploadImagesAsync(imageFiles, listingId);
            }
            catch (Exception ex)
            {
                // Never let an upload failure surface as a generic 500 / silent
                // failure - log full detail server-side, tell the seller plainly
                // that nothing was published, so they don't assume it worked.
                _logger.LogError(ex, "Image upload failed while creating listing {ListingId} for seller {SellerId}", listingId, sellerId);
                return OperationResult<string>.Fail(
                    "We couldn't upload your photos, so the listing was NOT published. " +
                    "This is usually a Firebase Storage configuration issue on the server - please try again, and contact support if it keeps happening.");
            }

            var listing = new Listing
            {
                Id = listingId,
                CategoryId = category.Id,
                CategoryName = category.Name,
                Module = category.Module,
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
                Model = model.ProductModel?.Trim(),
                Colour = model.Colour?.Trim(),
                Size = model.Size?.Trim(),
                Specifications = model.Specifications?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>(),
                ImageUrls = imageUrls,
                SellerId = sellerId,
                SellerName = sellerName,
                Status = ListingStatus.Active.ToString()
            };

            try
            {
                await _listingRepository.AddAsync(listing);
            }
            catch (Exception ex)
            {
                // The images already uploaded successfully at this point, but the
                // Firestore write itself failed - this is the scenario the spec
                // calls out explicitly ("never fail silently"). Log everything
                // needed to diagnose it, and tell the seller clearly that the
                // listing was NOT created rather than leaving them guessing.
                _logger.LogError(ex,
                    "Firestore write failed while creating listing {ListingId} for seller {SellerId}. Uploaded image URLs: {ImageUrls}",
                    listingId, sellerId, string.Join(", ", imageUrls));
                return OperationResult<string>.Fail(
                    "We uploaded your photos but couldn't save the listing itself, so it was NOT published. " +
                    "Please try again - if this keeps happening, contact support and mention the time you tried.");
            }

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

            List<string> newlyUploadedUrls;
            try
            {
                newlyUploadedUrls = newImageFiles.Count > 0
                    ? await UploadImagesAsync(newImageFiles, listing.Id)
                    : new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed while updating listing {ListingId}", listing.Id);
                return OperationResult.Fail(
                    "We couldn't upload your new photos, so none of your changes were saved. Please try again.");
            }

            listing.CategoryId = category.Id;
            listing.CategoryName = category.Name;
            listing.Module = category.Module;
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
            listing.Model = model.ProductModel?.Trim();
            listing.Colour = model.Colour?.Trim();
            listing.Size = model.Size?.Trim();
            listing.Specifications = model.Specifications?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList() ?? new List<string>();
            listing.ImageUrls = model.ExistingImageUrls.Concat(newlyUploadedUrls).ToList();

            try
            {
                await _listingRepository.UpdateAsync(listing.Id, listing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firestore write failed while updating listing {ListingId}", listing.Id);
                return OperationResult.Fail(
                    "We couldn't save your changes. Please try again - if this keeps happening, contact support.");
            }

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
