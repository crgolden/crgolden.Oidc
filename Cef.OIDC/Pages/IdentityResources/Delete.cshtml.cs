namespace Cef.OIDC.Pages.IdentityResources
{
    using System.Threading.Tasks;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Interfaces;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly IConfigurationDbContext _context;

        public DeleteModel(IConfigurationDbContext context)
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

            IdentityResource = await _context.IdentityResources.SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (IdentityResource == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (IdentityResource.Id <= 0)
            {
                return NotFound();
            }

            var identityResource = await _context.IdentityResources.SingleOrDefaultAsync(x => x.Id == IdentityResource.Id);
            if (identityResource == null)
            {
                return Page();
            }

            _context.IdentityResources.Remove(identityResource);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
