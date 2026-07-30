using System.ComponentModel.DataAnnotations;

namespace VarsityLoop.Models.ViewModels.Admin
{
    public class CategoryViewModel
    {
        public string? Id { get; set; }

        [Required, StringLength(60)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Range(0, 999)]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Required]
        [Display(Name = "Marketplace Module")]
        public VarsityLoop.Models.Entities.CategoryModule Module { get; set; } = VarsityLoop.Models.Entities.CategoryModule.Books;
    }
}
