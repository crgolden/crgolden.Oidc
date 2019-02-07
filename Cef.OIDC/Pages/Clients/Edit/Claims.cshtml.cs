namespace Cef.OIDC.Pages.Clients.Edit
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
    public class ClaimsModel : PageModel
    {
        private readonly IConfigurationDbContext _context;

        public ClaimsModel(IConfigurationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Client Client { get; set; }

        public IEnumerable<ClientClaim> Claims { get; set; }

        public async Task<IActionResult> OnGetAsync([FromRoute] int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            Client = await _context.Clients
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.Id.Equals(id));
            if (Client == null)
            {
                return NotFound();
            }

            Claims = Client.Claims
                .Select(x => new ClientClaim
                {
                    Id = x.Id,
                    Type = x.Type,
                    Value = x.Value
                });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || Client.Id <= 0)
            {
                return Page();
            }

            var client = await _context.Clients
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.Id.Equals(Client.Id));
            if (client == null)
            {
                return Page();
            }

            client.Updated = DateTime.UtcNow;
            if (Client.Claims != null)
            {

                client.Claims.AddRange(Client.Claims.Where(x => !client.Claims.Any(y => y.Type.Equals(x.Type))));
                var claims = client.Claims.Where(x => !Client.Claims.Any(y => y.Type.Equals(x.Type))).ToHashSet();
                foreach (var claim in claims)
                {
                    client.Claims.Remove(claim);
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                client.Claims = new List<ClientClaim>();
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("../Details/Claims", new { Client.Id });
        }
    }
}
