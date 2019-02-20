namespace Clarity.Oidc.ViewModels.AccountViewModels
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class ConfirmEmailViewModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}
