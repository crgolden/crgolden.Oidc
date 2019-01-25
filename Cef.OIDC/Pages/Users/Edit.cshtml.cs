﻿namespace Cef.OIDC.Pages.Users
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Kendo.Mvc.Extensions;
    using Kendo.Mvc.UI;
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
        public User UserModel { get; set; }

        public IEnumerable<Role> Roles { get; set; }

        public IEnumerable<UserClaim> Claims { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                return NotFound();
            }

            UserModel = await _userManager.Users
                .Include(x => x.UserRoles)
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (UserModel == null)
            {
                return NotFound();
            }

            Roles = _roleManager.Roles
                .Select(x => new Role
                {
                    Id = x.Id,
                    Name = x.Name
                });
            Claims = UserModel.Claims
                .Where(x => !x.ClaimType.Equals(ClaimTypes.Role))
                .Select(x => new UserClaim
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    ClaimType = x.ClaimType,
                    ClaimValue = x.ClaimValue
                });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!ModelState.IsValid || id.Equals(Guid.Empty))
            {
                return Page();
            }

            var user = await _userManager.Users
                .Include(x => x.UserRoles)
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (user == null)
            {
                return Page();
            }

            user.Email = UserModel.Email;
            user.UserName = UserModel.Email;
            if (UserModel.UserRoles != null)
            {
                foreach (var userRole in UserModel.UserRoles
                    .Where(x => !user.UserRoles.Any(y => y.RoleId.Equals(x.RoleId))))
                {
                    var role = await _roleManager.FindByIdAsync($"{userRole.RoleId}");
                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, role.Name));
                    user.UserRoles.Add(userRole);
                }

                var userRoles = user.UserRoles
                    .Where(x => !UserModel.UserRoles.Any(y => y.RoleId.Equals(x.RoleId)))
                    .ToHashSet();
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
                    .Select(x => new Claim(x.ClaimType, x.ClaimValue)));
            }

            await _userManager.UpdateAsync(user);
            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostCreateUserClaimAsync([DataSourceRequest] DataSourceRequest request, UserClaim userClaim)
        {
            var user = await _userManager.FindByIdAsync($"{userClaim.UserId}");
            var claims = await _userManager.GetClaimsAsync(user);
            var claim = claims.SingleOrDefault(x => x.Type == userClaim.ClaimType);
            if (claim != null)
            {
                return new BadRequestObjectResult($"Claim '{userClaim.ClaimType}' already exists");
            }

            await _userManager.AddClaimAsync(user, userClaim.ToClaim());
            return new OkObjectResult(new[] { userClaim }.ToDataSourceResult(request, ModelState));
        }

        public async Task<IActionResult> OnPostEditUserClaimAsync([DataSourceRequest] DataSourceRequest request, UserClaim userClaim)
        {
            var user = await _userManager.FindByIdAsync($"{userClaim.UserId}");
            var claims = await _userManager.GetClaimsAsync(user);
            var claim = claims.SingleOrDefault(x => x.Type == userClaim.ClaimType);
            if (claim == null)
            {
                return new BadRequestObjectResult($"Claim '{userClaim.ClaimType}' does not exist");
            }

            await _userManager.ReplaceClaimAsync(user, claim, userClaim.ToClaim());
            return new OkObjectResult(new[] { userClaim }.ToDataSourceResult(request, ModelState));
        }

        public async Task<IActionResult> OnPostDeleteUserClaimAsync([DataSourceRequest] DataSourceRequest request, UserClaim userClaim)
        {
            var user = await _userManager.FindByIdAsync($"{userClaim.UserId}");
            var claims = await _userManager.GetClaimsAsync(user);
            var claim = claims.SingleOrDefault(x => x.Type == userClaim.ClaimType);
            if (claim == null)
            {
                return new BadRequestObjectResult($"Claim '{userClaim.ClaimType}' does not exist");
            }

            await _userManager.RemoveClaimAsync(user, claim);
            return new OkObjectResult(new[] { userClaim }.ToDataSourceResult(request, ModelState));
        }
    }
}
