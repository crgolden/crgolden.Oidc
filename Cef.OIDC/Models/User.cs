namespace Cef.OIDC.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Identity;
    using Relationships;

    [ExcludeFromCodeCoverage]
    public class User : IdentityUser<Guid>
    {
        public User()
        {
        }

        public User(string userName) : base(userName)
        {
        }

        public virtual ICollection<UserClaim> Claims { get; set; }

        public virtual ICollection<UserLogin> Logins { get; set; }

        public virtual ICollection<UserToken> Tokens { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}