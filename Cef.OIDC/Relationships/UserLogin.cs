namespace Cef.OIDC.Relationships
{
    using System;
    using Microsoft.AspNetCore.Identity;
    using Models;

    public class UserLogin : IdentityUserLogin<Guid>
    {
        public virtual User User { get; set; }
    }
}
