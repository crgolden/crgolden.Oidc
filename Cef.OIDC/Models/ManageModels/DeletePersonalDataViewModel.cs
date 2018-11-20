namespace Cef.OIDC.Models.ManageModels
{
    using System.ComponentModel.DataAnnotations;

    public class DeletePersonalDataViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RequirePassword { get; set; }
    }
}
