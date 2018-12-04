namespace Cef.OIDC.Models.ManageModels
{
    using System.ComponentModel.DataAnnotations;

    public class ProfileViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool EmailConfirmed { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
