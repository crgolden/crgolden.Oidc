namespace Clarity.Oidc.ViewModels.ManageViewModels
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;

    [ExcludeFromCodeCoverage]
    public class ExternalLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        public IList<AuthenticationScheme> OtherLogins { get; set; }

        public bool ShowRemoveButton { get; set; }
    }
}
