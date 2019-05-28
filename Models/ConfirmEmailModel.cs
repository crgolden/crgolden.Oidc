namespace Clarity.Oidc
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class ConfirmEmailModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}
