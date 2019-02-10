﻿namespace Cef.OIDC.Pages.ApiResources.Edit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    [Authorize(Roles = "Admin")]
    public class ClaimTypesModel : PageModel
    {
        private readonly IConfigurationDbContext _context;

        public ClaimTypesModel(IConfigurationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ApiResource ApiResource { get; set; }

        public IEnumerable<ApiResourceClaim> ClaimTypes { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            ApiResource = await _context.ApiResources
                .Include(x => x.UserClaims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));

            if (ApiResource == null)
            {
                return NotFound();
            }

            ClaimTypes = ApiResource.UserClaims
                .Select(x => new ApiResourceClaim
                {
                    Id = x.Id,
                    Type = x.Type
                });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApiResource.Id <= 0)
            {
                return Page();
            }

            var apiResource = await _context.ApiResources
                .Include(x => x.UserClaims)
                .SingleOrDefaultAsync(x => x.Id.Equals(ApiResource.Id));

            if (apiResource == null)
            {
                return Page();
            }

            if (ApiResource.UserClaims != null)
            {
                foreach (var apiResourceClaimType in ApiResource.UserClaims.Where(x => x.Id > 0))
                {
                    var claimType = apiResource.UserClaims.Single(x => x.Id.Equals(apiResourceClaimType.Id));
                    claimType.Type = apiResourceClaimType.Type;
                }

                apiResource.UserClaims.AddRange(ApiResource.UserClaims.Where(x => x.Id == 0));
                var claimTypes = apiResource.UserClaims.Where(x => !ApiResource.UserClaims.Any(y => y.Id.Equals(x.Id))).ToHashSet();
                foreach (var claimType in claimTypes)
                {
                    apiResource.UserClaims.Remove(claimType);
                }
            }
            else
            {
                apiResource.UserClaims = new List<ApiResourceClaim>();
            }

            apiResource.Updated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToPage("../Details/ClaimTypes", new { ApiResource.Id });
        }
    }
}
