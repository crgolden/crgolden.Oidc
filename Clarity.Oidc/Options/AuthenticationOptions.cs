namespace Clarity.Oidc.Options
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class AuthenticationOptions
    {
        public FacebookOptions FacebookOptions { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class FacebookOptions
    {
        public string AppId { get; set; }

        public string AppSecret { get; set; }
    }
}