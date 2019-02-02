namespace Cef.OIDC.Pages.Users.Details
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

    [Authorize(Roles = "Admin")]
    public class RolesModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public RolesModel(UserManager<User> userManager)
        {
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
                .ThenInclude(x => x.Role)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (UserModel == null)
            {
                return NotFound();
            }

            Roles = UserModel.UserRoles.Select(x => new Role
            {
                Id = x.Role.Id,
                Name = x.Role.Name
            });

            return Page();
        }
    }
}
