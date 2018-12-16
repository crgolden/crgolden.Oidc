namespace Cef.OIDC.Pages.Account
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        [TempData]
        [ViewData]
        public string Origin { get; set; }

        public void OnGet()
        {
        }
    }
}
