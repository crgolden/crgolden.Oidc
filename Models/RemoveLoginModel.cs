namespace Clarity.Oidc
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class RemoveLoginModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }
}
