namespace Clarity.Oidc.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Identity;
    using Relationships;

    [ExcludeFromCodeCoverage]
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