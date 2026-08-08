using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VarsityLoop.Models.Entities;

namespace VarsityLoop.Models.ViewModels.Accommodation
{
    public class AccommodationFormViewModel
    {
        public string? Id { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Residence Name")]
        public string ResidenceName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Residence Classification")]
        public ResidenceClassification Classification { get; set; } = ResidenceClassification.Private;

        [Required]
        [Display(Name = "Accommodation Type")]
        public AccommodationType AccommodationType { get; set; } = AccommodationType.SingleRoom;

        [Required, Range(0, 100000)]
        [Display(Name = "Monthly Rent")]
        public double MonthlyRent { get; set; }

        [Range(0, 100000)]
        public double Deposit { get; set; }

        [Required, StringLength(150)]
        public string University { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Distance From Campus")]
        public string? DistanceFromCampus { get; set; }

        [Required]
        [Display(Name = "Available From")]
        [DataType(DataType.Date)]
        public DateTime AvailableFrom { get; set; } = DateTime.UtcNow.Date;

        [StringLength(60)]
        [Display(Name = "Lease Period")]
        public string? LeasePeriod { get; set; }

        [Required]
        [Display(Name = "Gender Preference")]
        public GenderPreference GenderPreference { get; set; } = GenderPreference.Any;

        [Required, StringLength(3000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Google Maps Link")]
        [Url]
        public string? GoogleMapsUrl { get; set; }

        [Display(Name = "Gallery Photos")]
        public List<IFormFile>? GalleryFiles { get; set; }

        public List<string> ExistingGalleryUrls { get; set; } = new();
    }
}
