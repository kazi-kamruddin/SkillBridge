using System.ComponentModel.DataAnnotations;

namespace SkillBridge.Models
{
    public class LoginViewModel
    {
        private string email;

        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get => email; set => email = value?.Trim(); }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        private string email;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get => email; set => email = value?.Trim(); }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        private string email;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get => email; set => email = value?.Trim(); }
    }

    public class ResetPasswordViewModel
    {
        private string email;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get => email; set => email = value?.Trim(); }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(2048)]
        public string Code { get; set; }
    }
}
