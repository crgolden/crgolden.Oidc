namespace Cef.OIDC.Models.AccountModels
{
    using System.ComponentModel.DataAnnotations;

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

        public string Message { get; set; }

        public bool Succeeded { get; set; }

        public bool IsLockedOut { get; set; }

        public bool RequiresTwoFactor { get; set; }

        public bool IsNotAllowed { get; set; }
    }
}