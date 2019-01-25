namespace Cef.OIDC.Pages.Users
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

    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public DetailsModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public User UserModel { get; set; }

        public IEnumerable<Role> Roles { get; set; }

        public IEnumerable<Claim> Claims { get; set; }

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
            Claims = await _userManager.GetClaimsAsync(UserModel);

            return Page();
        }
    }
}
