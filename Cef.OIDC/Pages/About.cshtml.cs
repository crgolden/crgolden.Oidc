namespace Cef.OIDC.Pages
{
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class AboutModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "Your application description page.";
        }
    }
}
