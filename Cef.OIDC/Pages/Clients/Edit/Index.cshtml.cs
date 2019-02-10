namespace Cef.OIDC.Pages.Clients.Edit
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
        public Client Client { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Client = await _context.Clients.FindAsync(id);
            if (Client == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Client.Id <= 0)
            {
                return Page();
            }

            var client = await _context.Clients.FindAsync(Client.Id);
            if (client == null)
            {
                return Page();
            }

            client.ClientName = Client.ClientName;
            client.Updated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToPage("../Details/Index", new { Client.Id });
        }
    }
}
