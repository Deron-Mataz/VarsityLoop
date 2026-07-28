using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;
using VarsityLoop.Repositories.Interfaces;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly IFirestoreRepository<Category> _repository;

        public CategoryService(IFirestoreRepository<Category> repository)
        {
            _repository = repository;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            var all = await _repository.GetAllAsync();
            return all.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
        }

        public Task<Category?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);

        public async Task<OperationResult> CreateAsync(string name, string? description, int displayOrder)
        {
            name = name.Trim();

            var existing = await GetAllAsync();
            if (existing.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return OperationResult.Fail("A category with this name already exists.");
            }

            await _repository.AddAsync(new Category
            {
                Name = name,
                Description = description?.Trim(),
                DisplayOrder = displayOrder
            });

            return OperationResult.Ok();
        }

        public async Task<OperationResult> UpdateAsync(string id, string name, string? description, int displayOrder)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return OperationResult.Fail("Category not found.");

            name = name.Trim();

            var existing = await GetAllAsync();
            if (existing.Any(c => c.Id != id && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return OperationResult.Fail("A category with this name already exists.");
            }

            category.Name = name;
            category.Description = description?.Trim();
            category.DisplayOrder = displayOrder;

            await _repository.UpdateAsync(id, category);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return OperationResult.Fail("Category not found.");

            // Listings that already reference this category keep their denormalized
            // CategoryName and keep displaying fine - they just won't be
            // re-selectable under this category once it's gone from the list.
            await _repository.SoftDeleteAsync(id);
            return OperationResult.Ok();
        }
    }
}
