namespace Clarity.Oidc.Pages.Account
{
    using System.Threading.Tasks;
    using Core;
    using Extensions;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;
    using Models;

    public class VerifyEmailModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ExternalLoginModel> _logger;

        public VerifyEmailModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IEmailService emailService,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
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

            await _emailService.SendConfirmationEmailAsync(
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
