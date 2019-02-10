namespace Cef.OIDC.Relationships
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Identity;
    using Models;

    [ExcludeFromCodeCoverage]
    public class UserLogin : IdentityUserLogin<Guid>
    {
        public virtual User User { get; set; }
    }
}
