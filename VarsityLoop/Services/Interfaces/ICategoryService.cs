using VarsityLoop.Models.Common;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(string id);
        Task<OperationResult> CreateAsync(string name, string? description, int displayOrder, CategoryModule module, string iconClass);
        Task<OperationResult> UpdateAsync(string id, string name, string? description, int displayOrder, CategoryModule module, string iconClass);
        Task<OperationResult> DeleteAsync(string id);
    }
}
