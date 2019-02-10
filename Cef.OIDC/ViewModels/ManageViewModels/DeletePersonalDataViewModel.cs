namespace Cef.OIDC.ViewModels.ManageViewModels
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class DeletePersonalDataViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
