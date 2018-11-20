namespace Cef.OIDC.Models.AccountModels
{
    using System.ComponentModel.DataAnnotations;

    public class LoginWith2faViewModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }

        public string Message { get; set; }

        public bool Succeeded { get; set; }

        public bool IsLockedOut { get; set; }
    }
}
