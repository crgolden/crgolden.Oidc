namespace Cef.OIDC.Pages.Account
{
    using System.Threading.Tasks;
    using Core.Models;
    using Extensions;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    public class VerifyEmailModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public VerifyEmailModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IEmailSender emailSender,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public string ReturnUrl { get; set; }

        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        [ViewData]
        public string Origin { get; set; }

        public void OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null, string origin = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            TempData[nameof(Origin)] = origin;

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { returnUrl });
            }

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user == null)
            {
                ErrorMessage = $"Unable to load user with ID '{_userManager.GetUserId(info.Principal)}'.";
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            await _emailSender.SendConfirmationEmailAsync(
                userId: user.Id,
                email: user.Email,
                origin: origin,
                code: await _userManager.GenerateEmailConfirmationTokenAsync(user));
            _logger.LogInformation($"Email verification email sent to '{user.Email}'.");

            SuccessMessage = "Verification email sent. Please check your email.";
            Origin = origin;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
