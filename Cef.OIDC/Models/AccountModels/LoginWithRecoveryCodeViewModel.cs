namespace Cef.OIDC.Models.AccountModels
{
    using System.ComponentModel.DataAnnotations;

    public class LoginWithRecoveryCodeViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; }

        public string Message { get; set; }

        public bool Succeeded { get; set; }

        public bool IsLockedOut { get; set; }
    }
}
