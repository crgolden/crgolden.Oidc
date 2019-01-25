namespace Cef.OIDC.Pages.Roles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Models;
    using Relationships;

    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public EditModel(
            RoleManager<Role> roleManager,
            UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [BindProperty]
        public Role Role { get; set; }

        public IEnumerable<User> Users { get; set; }

        public IEnumerable<Claim> Claims { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                return NotFound();
            }

            Role = await _roleManager.Roles
                .Include(x => x.UserRoles)
                .Include(x => x.RoleClaims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (Role == null)
            {
                return NotFound();
            }

            Role.UserRoles = Role.UserRoles.Select(x => new UserRole
            {
                UserId = x.UserId
            }).ToList();
            Users = _userManager.Users;
            Claims = await _roleManager.GetClaimsAsync(Role);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!ModelState.IsValid || id.Equals(Guid.Empty))
            {
                return Page();
            }

            var role = await _roleManager.Roles
                .Include(x => x.UserRoles)
                .Include(x => x.RoleClaims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (role == null)
            {
                return Page();
            }

            role.Name = Role.Name;
            if (Role.UserRoles != null)
            {
                foreach (var userRole in Role.UserRoles.Where(x => !role.UserRoles.Any(y => y.UserId.Equals(x.UserId))))
                {
                    role.UserRoles.Add(userRole);
                    var user = await _userManager.FindByIdAsync($"{userRole.UserId}");
                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, Role.Name));
                }

                var userRoles = role.UserRoles.Where(x => !Role.UserRoles.Any(y => y.UserId.Equals(x.UserId))).ToHashSet();
                foreach (var userRole in userRoles)
                {
                    var user = await _userManager.FindByIdAsync($"{userRole.UserId}");
                    await _userManager.RemoveClaimAsync(user, new Claim(ClaimTypes.Role, Role.Name));
                    role.UserRoles.Remove(userRole);
                }
            }
            else
            {
                role.UserRoles = new List<UserRole>();
            }
            await _roleManager.UpdateAsync(role);
            return RedirectToPage("./Details", new { id });
        }
    }
}
