namespace Cef.OIDC.Pages.IdentityResources.Edit
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
        public IdentityResource IdentityResource { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            IdentityResource = await _context.IdentityResources.FindAsync(id);
            if (IdentityResource == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || IdentityResource.Id <= 0)
            {
                return Page();
            }

            var identityResource = await _context.IdentityResources.FindAsync(IdentityResource.Id);
            if (identityResource == null)
            {
                return Page();
            }

            identityResource.Name = IdentityResource.Name;
            identityResource.DisplayName = IdentityResource.DisplayName;
            identityResource.Description = identityResource.Description;
            identityResource.Enabled = IdentityResource.Enabled;
            identityResource.Emphasize = IdentityResource.Emphasize;
            identityResource.Required = IdentityResource.Required;
            identityResource.ShowInDiscoveryDocument = IdentityResource.ShowInDiscoveryDocument;
            identityResource.NonEditable = IdentityResource.NonEditable;
            identityResource.Updated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToPage("../Details/Index", new { IdentityResource.Id });
        }
    }
}
