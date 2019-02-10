namespace Cef.OIDC.Options
{
    using System.Diagnostics.CodeAnalysis;
    using Core;

    [ExcludeFromCodeCoverage]
    public class UserOptions
    {
        public UserOption[] Users { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class UserOption
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public AddressClaim Address { get; set; }
    }
}
