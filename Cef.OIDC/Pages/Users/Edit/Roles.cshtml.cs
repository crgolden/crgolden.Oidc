namespace Cef.OIDC.Pages.Users.Edit
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
    public class RolesModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public RolesModel(
            RoleManager<Role> roleManager,
            UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [BindProperty]
        public User UserModel { get; set; }

        public IEnumerable<Role> Roles { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                return NotFound();
            }

            UserModel = await _userManager.Users
                .Include(x => x.UserRoles)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (UserModel == null)
            {
                return NotFound();
            }

            Roles = _roleManager.Roles.Select(x => new Role
            {
                Id = x.Id,
                Name = x.Name
            });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || UserModel.Id.Equals(Guid.Empty))
            {
                return Page();
            }

            var user = await _userManager.Users
                .Include(x => x.UserRoles)
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.Id.Equals(UserModel.Id));
            if (user == null)
            {
                return Page();
            }

            if (UserModel.UserRoles != null)
            {
                foreach (var userRole in UserModel.UserRoles.Where(x => !user.UserRoles.Any(y => y.RoleId.Equals(x.RoleId))))
                {
                    var role = await _roleManager.FindByIdAsync($"{userRole.RoleId}");
                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, role.Name));
                    user.UserRoles.Add(userRole);
                }

                var userRoles = user.UserRoles.Where(x => !UserModel.UserRoles.Any(y => y.RoleId.Equals(x.RoleId))).ToHashSet();
                foreach (var userRole in userRoles)
                {
                    var role = await _roleManager.FindByIdAsync($"{userRole.RoleId}");
                    await _userManager.RemoveClaimAsync(user, new Claim(ClaimTypes.Role, role.Name));
                    user.UserRoles.Remove(userRole);
                }
            }
            else
            {
                user.UserRoles = new List<UserRole>();
                await _userManager.RemoveClaimsAsync(user, user.Claims
                    .Where(x => x.ClaimType.Equals(ClaimTypes.Role))
                    .Select(x => x.ToClaim()));
            }

            await _userManager.UpdateAsync(user);
            return RedirectToPage("../Details/Users", new { UserModel.Id });
        }
    }
}
