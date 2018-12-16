namespace Cef.OIDC.Relationships
{
    using System;
    using Microsoft.AspNetCore.Identity;
    using Models;

    public class RoleClaim : IdentityRoleClaim<Guid>
    {
        public virtual Role Role { get; set; }
    }
}
