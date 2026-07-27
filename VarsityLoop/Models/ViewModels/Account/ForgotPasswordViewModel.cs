using System.ComponentModel.DataAnnotations;

namespace VarsityLoop.Models.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
