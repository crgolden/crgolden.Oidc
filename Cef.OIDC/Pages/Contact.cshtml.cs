namespace Cef.OIDC.Pages
{
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class ContactModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "Your contact page.";
        }
    }
}
