namespace Clarity.Oidc
{
    using System.ComponentModel.DataAnnotations;

    public class ConfirmEmailModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}
