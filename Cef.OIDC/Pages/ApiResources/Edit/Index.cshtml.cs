namespace Cef.OIDC.Pages.ApiResources.Edit
{
    using System;
    using System.Threading.Tasks;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IConfigurationDbContext _context;

        public IndexModel(IConfigurationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ApiResource ApiResource { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            ApiResource = await _context.ApiResources.FindAsync(id);
            if (ApiResource == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || ApiResource.Id <= 0)
            {
                return Page();
            }

            var apiResource = await _context.ApiResources.FindAsync(ApiResource.Id);
            if (apiResource == null)
            {
                return Page();
            }

            apiResource.Name = ApiResource.Name;
            apiResource.DisplayName = ApiResource.DisplayName;
            apiResource.Description = apiResource.Description;
            apiResource.Enabled = ApiResource.Enabled;
            apiResource.LastAccessed = ApiResource.LastAccessed;
            apiResource.NonEditable = ApiResource.NonEditable;
            apiResource.Updated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToPage("../Details/Index", new { ApiResource.Id });
        }
    }
}
