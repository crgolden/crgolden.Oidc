namespace Cef.OIDC.ViewModels.ManageViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class DeletePersonalDataViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
