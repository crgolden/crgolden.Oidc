namespace Cef.OIDC.Options
{
    public class AuthenticationOptions
    {
        public Facebook Facebook { get; set; }
    }

    public class Facebook
    {
        public string AppId { get; set; }

        public string AppSecret { get; set; }
    }
}