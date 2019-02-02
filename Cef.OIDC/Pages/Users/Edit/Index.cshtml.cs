﻿namespace Cef.OIDC.Pages.Users.Edit
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Models;

    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public IndexModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public User UserModel { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                return NotFound();
            }

            UserModel = await _userManager.FindByIdAsync($"{id}");
            if (UserModel == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || UserModel.Id.Equals(Guid.Empty))
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync($"{UserModel.Id}");
            if (user == null)
            {
                return Page();
            }

            user.Email = UserModel.Email;
            user.UserName = UserModel.Email;
            await _userManager.UpdateAsync(user);
            return RedirectToPage("../Details/Index", new { UserModel.Id });
        }
    }
}