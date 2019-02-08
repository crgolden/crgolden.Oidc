namespace Cef.OIDC.Pages.Roles.Details
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Relationships;

    [Authorize(Roles = "Admin")]
    public class ClaimsModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;

        public ClaimsModel(RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
        }

        [BindProperty]
        public Role Role { get; set; }

        public IEnumerable<RoleClaim> Claims { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                return NotFound();
            }

            Role = await _roleManager.Roles
                .Include(x => x.RoleClaims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (Role == null)
            {
                return NotFound();
            }

            Claims = Role.RoleClaims
                .Select(x => new RoleClaim
                {
                    ClaimType = x.ClaimType,
                    ClaimValue = x.ClaimValue
                });

            return Page();
        }
    }
}
