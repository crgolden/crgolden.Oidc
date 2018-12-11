namespace Cef.OIDC.Options
{
    public class UserOptions
    {
        public UserOption[] Users { get; set; }
    }

    public class UserOption
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }
    }
}
