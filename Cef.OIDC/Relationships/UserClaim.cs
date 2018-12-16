namespace Cef.OIDC.Relationships
{
    using System;
    using Microsoft.AspNetCore.Identity;
    using Models;

    public class UserClaim : IdentityUserClaim<Guid>
    {
        public virtual User User { get; set; }
    }
}
