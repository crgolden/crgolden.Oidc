namespace Cef.OIDC.Options
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class AuthenticationOptions
    {
        public Facebook Facebook { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class Facebook
    {
        public string AppId { get; set; }

        public string AppSecret { get; set; }
    }
}