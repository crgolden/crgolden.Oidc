namespace Cef.OIDC.Relationships
{
    using System;
    using Microsoft.AspNetCore.Identity;
    using Models;

    public class UserRole : IdentityUserRole<Guid>
    {
        public virtual User User { get; set; }

        public virtual Role Role { get; set; }
    }
}
