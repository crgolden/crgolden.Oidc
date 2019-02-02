﻿namespace Cef.OIDC.Pages.Roles.Edit
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
                    Id = x.Id,
                    RoleId = x.RoleId,
                    ClaimType = x.ClaimType,
                    ClaimValue = x.ClaimValue
                });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || Role.Id.Equals(Guid.Empty))
            {
                return Page();
            }

            var role = await _roleManager.Roles
                .Include(x => x.RoleClaims)
                .SingleOrDefaultAsync(x => x.Id.Equals(Role.Id));
            if (role == null)
            {
                return Page();
            }

            if (Role.RoleClaims != null)
            {
                foreach (var roleClaim in Role.RoleClaims.Where(x => !role.RoleClaims.Any(y => y.ClaimType.Equals(x.ClaimType))))
                {
                    await _roleManager.AddClaimAsync(role, roleClaim.ToClaim());
                }

                var roleClaims = role.RoleClaims.Where(x => !Role.RoleClaims.Any(y => y.ClaimType.Equals(x.ClaimType))).ToHashSet();
                foreach (var roleClaim in roleClaims)
                {
                    await _roleManager.RemoveClaimAsync(role, roleClaim.ToClaim());
                }
            }
            else
            {
                var roleClaims = role.RoleClaims.ToHashSet();
                foreach (var roleClaim in roleClaims)
                {
                    await _roleManager.RemoveClaimAsync(role, roleClaim.ToClaim());
                }
            }

            return RedirectToPage("../Details/Claims", new { Role.Id });
        }
    }
}