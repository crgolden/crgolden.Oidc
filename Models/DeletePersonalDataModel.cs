namespace crgolden.Oidc
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class DeletePersonalDataModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
