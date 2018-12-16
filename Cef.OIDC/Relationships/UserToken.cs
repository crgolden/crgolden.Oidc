namespace Cef.OIDC.Relationships
{
    using System;
    using Microsoft.AspNetCore.Identity;
    using Models;

    public class UserToken : IdentityUserToken<Guid>
    {
        public virtual User User { get; set; }
    }
}
