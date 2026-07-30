using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VarsityLoop.Models.ViewModels.Account
{
    public class ProfileViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Surname")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string University { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Biography { get; set; }

        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "New Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; }

        /// <summary>Set when the user picks one of the 6 preset avatars instead of uploading a custom photo.</summary>
        public int? SelectedAvatar { get; set; }
    }
}
