namespace Clarity.Oidc
{
    using System.ComponentModel.DataAnnotations;

    public class DeletePersonalDataModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
