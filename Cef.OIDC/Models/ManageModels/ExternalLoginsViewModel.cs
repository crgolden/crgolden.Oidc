﻿namespace Cef.OIDC.Models.ManageModels
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;

    public class ExternalLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }

        public IList<AuthenticationScheme> OtherLogins { get; set; }

        public bool ShowRemoveButton { get; set; }
    }
}
