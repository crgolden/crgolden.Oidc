namespace Clarity.Oidc
{
    public class AuthenticationOptions
    {
        public FacebookOptions FacebookOptions { get; set; }
    }

    public class FacebookOptions
    {
        public string AppId { get; set; }

        public string AppSecret { get; set; }
    }
}