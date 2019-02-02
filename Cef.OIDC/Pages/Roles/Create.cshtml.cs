namespace Cef.OIDC.Pages.Roles
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Models;

    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public CreateModel(
            RoleManager<Role> roleManager,
            UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [BindProperty]
        public Role Role { get; set; }

        public IEnumerable<User> Users { get; set; }

        public IActionResult OnGet()
        {
            Users = _userManager.Users;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Role.UserRoles != null)
            {
                foreach (var userRole in Role.UserRoles)
                {
                    var user = await _userManager.FindByIdAsync($"{userRole.UserId}");
                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, Role.Name));
                }
            }

            await _roleManager.CreateAsync(Role);
            return RedirectToPage("./Details/Index", new { Role.Id });
        }
    }
}