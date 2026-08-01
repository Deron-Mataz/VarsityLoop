using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Listings
{
    public class ListingFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please choose a category.")]
        [Display(Name = "Category")]
        public string CategoryId { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0, 100000, ErrorMessage = "Enter a valid price.")]
        public double Price { get; set; }

        [StringLength(100)]
        public string? Author { get; set; }

        [StringLength(20)]
        [Display(Name = "ISBN")]
        public string? Isbn { get; set; }

        [StringLength(100)]
        public string? Course { get; set; }

        [StringLength(100)]
        public string? Faculty { get; set; }

        [Required]
        public ListingCondition Condition { get; set; } = ListingCondition.Good;

        [StringLength(60)]
        public string? Type { get; set; }

        [StringLength(60)]
        public string? Brand { get; set; }

        [StringLength(60)]
        public string? ProductModel { get; set; }

        [StringLength(40)]
        public string? Colour { get; set; }

        [StringLength(40)]
        public string? Size { get; set; }

        /// <summary>Free-text spec lines (e.g. "8GB RAM"), added/removed dynamically in the form.</summary>
        public List<string> Specifications { get; set; } = new();

        [Required, StringLength(150)]
        public string University { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Location { get; set; }

        /// <summary>New images to upload (Create: required at least one; Edit: optional additions).</summary>
        [Display(Name = "Photos")]
        public List<IFormFile>? ImageFiles { get; set; }

        /// <summary>Existing image URLs, carried through on Edit so the user can see/remove them.</summary>
        public List<string> ExistingImageUrls { get; set; } = new();
    }
}
