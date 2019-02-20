namespace Clarity.Oidc.Pages.Users.Details
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
    public class ClaimsModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public ClaimsModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public User UserModel { get; set; }

        public IEnumerable<UserClaim> Claims { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                return NotFound();
            }

            UserModel = await _userManager.Users
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (UserModel == null)
            {
                return NotFound();
            }

            Claims = UserModel.Claims
                .Where(x => !x.ClaimType.Equals(ClaimTypes.Role))
                .Select(x => new UserClaim
                {
                    ClaimType = x.ClaimType,
                    ClaimValue = x.ClaimValue
                });

            return Page();
        }
    }
}
