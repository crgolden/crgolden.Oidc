﻿namespace Cef.OIDC.Pages.Roles
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Models;

    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;

        public IndexModel(RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
        }

        public IEnumerable<Role> Roles { get; set; }

        public void OnGet()
        {
            Roles = _roleManager.Roles;
        }
    }
}