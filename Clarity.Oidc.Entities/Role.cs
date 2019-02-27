namespace Clarity.Oidc
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;

    public class Role : IdentityRole<Guid>
    {
        public Role()
        {
        }

        public Role(string roleName) : base(roleName)
        {
        }

        public virtual ICollection<UserRole> UserRoles { get; set; }

        public virtual ICollection<RoleClaim> RoleClaims { get; set; }
    }
}