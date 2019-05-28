namespace Clarity.Oidc
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;

    [ExcludeFromCodeCoverage]
    public class ExternalLoginsModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        public IList<AuthenticationScheme> OtherLogins { get; set; }

        public bool ShowRemoveButton { get; set; }
    }
}
