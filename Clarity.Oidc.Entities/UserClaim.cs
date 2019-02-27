namespace Clarity.Oidc
{
    using System;
    using Microsoft.AspNetCore.Identity;

    public class UserClaim : IdentityUserClaim<Guid>
    {
        public virtual User User { get; set; }
    }
}
