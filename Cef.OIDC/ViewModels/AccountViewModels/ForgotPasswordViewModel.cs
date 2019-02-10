namespace Cef.OIDC.ViewModels.AccountViewModels
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
