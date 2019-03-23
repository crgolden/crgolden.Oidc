namespace Clarity.Oidc
{
    using System.ComponentModel.DataAnnotations;
    using Core;

    public class ProfileModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool EmailConfirmed { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        public bool PhoneNumberConfirmed { get; set; }

        [Required]
        [Display(Name="First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public Address Address { get; set; }
    }
}
