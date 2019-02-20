namespace Clarity.Oidc.Relationships
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Identity;
    using Models;

    [ExcludeFromCodeCoverage]
    public class UserClaim : IdentityUserClaim<Guid>
    {
        public virtual User User { get; set; }
    }
}
