namespace crgolden.Oidc.Pages.Account
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Shared;

    [AllowAnonymous]
    public class VerifyEmailModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IQueueClient _emailQueueClient;
        private readonly ILogger<ExternalLoginModel> _logger;

        public VerifyEmailModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IEnumerable<IQueueClient> queueClients,
            IOptions<ServiceBusOptions> serviceBusOptions,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailQueueClient = queueClients.Single(x => x.QueueName == serviceBusOptions.Value.EmailQueueName);
            _logger = logger;
        }

        public string Email { get; set; }

        public string ReturnUrl { get; set; }

        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        [ViewData]
        public string Origin { get; set; }

        public void OnGetAsync(string email, string returnUrl = null)
        {
            Email = email;
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string email, string returnUrl = null, string origin = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            TempData[nameof(Origin)] = origin;

            var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (user == null)
            {
                ErrorMessage = $"Unable to load user with email '{email}'.";
                return RedirectToPage("./Login", new { ReturnUrl });
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
            await _emailQueueClient.SendConfirmationEmailAsync(
                userId: user.Id,
                email: user.Email,
                origin: origin,
                code: code).ConfigureAwait(false);
            _logger.LogInformation($"Email verification email sent to '{user.Email}'.");

            SuccessMessage = "Verification email sent. Please check your email.";
            Origin = origin;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
