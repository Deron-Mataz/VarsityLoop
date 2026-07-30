using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VarsityLoop.Models.Entities;
using VarsityLoop.Models.ViewModels.Admin;
using VarsityLoop.Services.Interfaces;

namespace VarsityLoop.Controllers
{
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    [Route("Admin/Categories")]
    public class AdminCategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;

        public AdminCategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Categories";
            var categories = await _categoryService.GetAllAsync();
            return View(categories);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewData["Title"] = "New Category";
            return View(new CategoryViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            ViewData["Title"] = "New Category";

            if (!ModelState.IsValid) return View(model);

            var result = await _categoryService.CreateAsync(model.Name, model.Description, model.DisplayOrder, model.Module);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't create category.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id}/Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return RedirectToAction(nameof(Index));

            ViewData["Title"] = "Edit Category";

            return View(new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DisplayOrder = category.DisplayOrder,
                Module = Enum.TryParse<VarsityLoop.Models.Entities.CategoryModule>(category.Module, out var m) ? m : VarsityLoop.Models.Entities.CategoryModule.Books
            });
        }

        [HttpPost("{id}/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CategoryViewModel model)
        {
            ViewData["Title"] = "Edit Category";

            if (!ModelState.IsValid) return View(model);

            var result = await _categoryService.UpdateAsync(id, model.Name, model.Description, model.DisplayOrder, model.Module);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Couldn't update category.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _categoryService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
