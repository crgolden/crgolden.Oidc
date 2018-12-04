namespace Cef.OIDC.Pages.Account
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    [AllowAnonymous]
    public class RedirectModel : PageModel
    {
        public string RedirectUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            RedirectUrl = returnUrl ?? Url.Content("~/");
        }
    }
}