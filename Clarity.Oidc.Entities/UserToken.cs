namespace Clarity.Oidc
{
    using System;
    using Microsoft.AspNetCore.Identity;

    public class UserToken : IdentityUserToken<Guid>
    {
        public virtual User User { get; set; }
    }
}
