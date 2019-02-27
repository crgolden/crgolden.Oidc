namespace Clarity.Oidc
{
    using System;
    using Microsoft.AspNetCore.Identity;

    public class UserLogin : IdentityUserLogin<Guid>
    {
        public virtual User User { get; set; }
    }
}
